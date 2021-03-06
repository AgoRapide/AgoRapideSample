﻿// Copyright (c) 2016, 2017 Bjørn Erling Fløtten, Trondheim, Norway
// MIT licensed. Details at https://github.com/AgoRapide/AgoRapide/blob/master/LICENSE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.ComponentModel;
using AgoRapide;
using AgoRapide.Core;
using AgoRapide.API;
using System.Reflection;
using AgoRapide.Database;

namespace AgoRapideSample {

    /// <summary>
    /// Contains methods that "must" always be implemented in your application. 
    /// </summary>
    public class HomeController : BaseController {

        [HttpGet]
        [OverrideAuthentication]
        [BasicAuthentication(AccessLevelUse = AccessLevel.Admin)]
        [APIMethod(
            Description = "Generates mock-data for -" + nameof(Person) + "- and -" + nameof(Car) + "-. Will generate 100*(percentilevalue^2) cars",
            S1 = nameof(GenerateMockData), S2 = SynchronizerP.SynchronizerMockSize)]
        public object GenerateMockData(string SynchronizerMockSize) {
            try {
                if (!TryGetRequest(SynchronizerMockSize, out var request, out var completeErrorResponse)) return completeErrorResponse;
                var percentileValue = request.Parameters.PV<Percentile>(SynchronizerP.SynchronizerMockSize.A()).Value;
                var maxCars = percentileValue * percentileValue * 100; // Note _VERY_ primitive use of Percentile-concept here, assuming 100P sizes are 4x 50P sizes.
                var maxN = new Dictionary<Type, int> {
                    { typeof(Person), maxCars / 2 },
                    { typeof(Car), maxCars },
                    // { typeof(Dealers), maxCars / 50 },
                };
                var allIds = new Dictionary<Type, List<long>>();
                void reconciler(Type type) {  // where T : BaseEntity, new()                
                    var retval = new List<long>();
                    // var type = typeof(T);
                    Log("Creating " + type.ToString());
                    var entities = BaseEntity.GetMockEntities(
                        type,
                        propertyPredicate: new Func<PropertyKey, bool>(p => {
                            if (p.Key.A.ExternalPrimaryKeyOf != null) return false; // TOOD: Maybe throw an exception here
                            if (p.Key.A.ForeignKeyOf != null) return false; // These will be added later
                            if (p.Key.A.SampleValues == null || p.Key.A.SampleValues.Length == 0) return false;
                            return true;
                        }),
                        maxN: maxN
                    );
                    Log(type + ", Count: " + entities.Count, request.Result); /// TODO: Use <see cref="BaseEntityWithLogAndCount.Count"/> instead. 
                    entities.ForEach(e => retval.Add(DB.CreateEntity(request.CurrentUser.Id, type, e.Properties, request.Result)));
                    allIds.Add(type, retval);

                    request.Result.AddProperty(CoreP.SuggestedUrl.A(), request.API.CreateAPIUrl(CoreAPIMethod.EntityIndex, type, new QueryIdAll()));
                }
                reconciler(typeof(Person));
                reconciler(typeof(Car));

                var r = new Random(maxCars); // Initialize with value giving predictable results every time
                allIds.ForEach(e => {
                    Log("Adding foreign keys to " + e.Key);
                    var properties = e.Key.GetChildProperties().Values.Where(p => p.Key.A.ForeignKeyOf != null);
                    properties.ForEach(p => {
                        var foreignIdsAvailable = allIds.GetValue(p.Key.A.ForeignKeyOf, () => "Possible resolution: Include " + p.Key.A.ForeignKeyOf + " when creating entities");
                        e.Value.ForEach(pid => {
                            var fid = foreignIdsAvailable[r.Next(0, foreignIdsAvailable.Count)];
                            DB.CreateProperty(request.CurrentUser.Id, pid, fid, p.PropertyKeyWithIndex, fid, request.Result);
                        });
                    });
                });

                request.Result.ResultCode = ResultCode.ok;
                // TODO: Clear log-data now (?)
                // request.Result.LogData.Clear();
                Log("\r\n" + string.Join("\r\n", maxN.Select(e => e.Key + ": " + e.Value)), request.Result); /// TODO: Use <see cref="BaseEntityWithLogAndCount.Count"/> instead. 
                return request.GetResponse();
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// TODO: MOVE THIS INTO AgoRapide.BaseController!
        /// TODO: That is, use <see cref="APIMethodOrigin.Autogenerated"/>) routing directly to relevant method in <see cref="BaseController"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [APIMethod(CoreMethod = CoreAPIMethod.RootIndex)]
        public object RootIndex() {
            try {
                if (!TryGetRequest(out var request, out var completeErrorResponse)) return completeErrorResponse;
                request.ForceHTMLResponse(); // It is much more user friendly to have HTML response always here. If JSON is needed it can always be obtained by querying api/Method/All or similar.
                // TODO: Replace this with dictionary with links
                // TODO: Like AllMethods, AllClassAndMethod, AllEnumClass
                return request.GetOKResponseAsMultipleEntities(APIMethod.AllMethods.Select(m => (BaseEntity)m).ToList());
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// Note how some of this method is implemented here in the application itself because of the application specific nature. 
        /// </summary>
        /// <param name="GeneralQueryId"></param>
        /// <returns></returns>
        [HttpGet]
        [OverrideAuthentication]
        [BasicAuthentication(AccessLevelUse = AccessLevel.User)]
        [APIMethod(
            Description = "Returns all -" + nameof(Person) + "- where one of -" + nameof(PersonP.FirstName) + "-, -" + nameof(PersonP.LastName) + "- or -" + nameof(PersonP.Email) + "- matches {" + nameof(CoreP.GeneralQueryId) + "}",
            S1 = nameof(GeneralQuery), S2 = CoreP.GeneralQueryId, CoreMethod = CoreAPIMethod.GeneralQuery)]
        public object GeneralQuery(string GeneralQueryId) {
            try {
                if (!TryGetRequest(GeneralQueryId, out var request, out var completeErrorResponse)) return completeErrorResponse;
                if (!GeneralQueryId.EndsWith("%")) GeneralQueryId += "%"; /// TODO: PostgreSQL specific? Where do we want to add this? /// TODO: Should we add a WILDCARD-parameter to <see cref="QueryIdKeyOperatorValue"/>. 
                return GeneralQuery<Person>(request, new QueryIdMultiple(new List<QueryId> {
                    new QueryIdKeyOperatorValue(PersonP.FirstName.A().Key, Operator.ILIKE, GeneralQueryId),  // Add all keys that you consider
                    new QueryIdKeyOperatorValue(PersonP.LastName.A().Key, Operator.ILIKE, GeneralQueryId),   // relevant for a general query here
                    new QueryIdKeyOperatorValue(PersonP.Email.A().Key, Operator.ILIKE, GeneralQueryId)       // (remember to optimize database correspondingly, like using partial indexes in PostgreSQL)
                })); /// TODO: Add a LIMIT parameter to <see cref="QueryIdMultiple"/>.
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        [HttpGet]
        [HttpPost]
        [APIMethod(
            S1 = nameof(AddFirstAdminUser), S2 = PersonP.Email, S3 = PersonP.Password, Description =
            "Adds the first administrative user to the system. Only allowed if no entities of type -" + nameof(Person) + "- exists",
            ShowDetailedResult = true)]
        public object AddFirstAdminUser(string Email, string Password) {
            try {
                if (!TryGetRequest(Email, Password, out var request, out var completeErrorResponse)) return completeErrorResponse;
                return AddFirstAdminUser<Person>(request);
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// TODO: MOVE THIS INTO AgoRapide.BaseController!
        /// TODO: Or rather, use <see cref="APIMethodOrigin.Autogenerated"/>) routing directly to relevant method in <see cref="BaseController"/>
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [HttpPost]
        [OverrideAuthentication]
        [BasicAuthentication(AccessLevelUse = AccessLevel.User)] // Stricter access like administrative access will be considered further downstream (by AgoRapideGenericMethod)
        [APIMethod(CoreMethod = CoreAPIMethod.GenericMethod)]
        public object GenericMethod() {
            try {
                var method = GetMethod();
                return AgoRapideGenericMethod(method, GetCurrentUser(method));
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }

        /// <summary>
        /// TODO: MOVE THIS INTO AgoRapide.BaseController!
        /// TODO: Or rather, use <see cref="APIMethodOrigin.Autogenerated"/>) routing directly to relevant method in <see cref="BaseController"/>
        /// </summary>
        /// <returns></returns>
        [OverrideAuthentication]
        [BasicAuthentication(AccessLevelUse = AccessLevel.Admin)]
        [HttpGet]
        [APIMethod(CoreMethod = CoreAPIMethod.ExceptionDetails, S1 = nameof(ExceptionDetails), AccessLevelUse = AccessLevel.Admin)]
        public object ExceptionDetails() {
            try {
                return HandleCoreMethodExceptionDetails(GetMethod());
            } catch (Exception ex) {
                return HandleExceptionAndGenerateResponse(ex);
            } finally {
                DBDispose();
            }
        }
    }
}
