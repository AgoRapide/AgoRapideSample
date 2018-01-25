"The shape of a program should reflect only the problem it needs to solve" - [Paul Graham - Revenge of the Nerds](http://www.paulgraham.com/icad.html).

Use AgoRapide and this goal comes within your reach!


# AgoRapideSample

Lightweight small framework for getting started with using AgoRapide. 

As of Jan 2018 the AgoRapideSample.sln (solution file) and AgoRapideSample.csproj (project file) references the AgoRapide library through a relative path.

AgoRapideSample and AgoRapide should therefore be cloned in a parallell directory structure, for instance like:
c:\git\AgoRapide
and
c:\git\AgoRapideSample

TODO: Reference the AgoRapide library as a Nuget package instead of a locally cloned copy of AgoRapide.

# FAQ

## IIS / IISExpress returns "403 - Forbidden: Access is denied"

IIS returns "403 - Forbidden: Access is denied" response when starting the AgoRapideSample application.

Most probably something went wrong quite early within the Startup.Configuration-method. 

Resolution: Check the log path for any exception information. 

Note that the log path defaults to @"c:\p\Logfiles\AgoRapideSample\AgoRapideLog_[DATE_HOUR].txt".

The following problems are common:

### Startup.GetEnvironment throws an UnknownEnvironmentException. 

Resolution: Adjust the code within Startup.GetEnvironment.

### PostgreSQLDatabase throws an OpenDatabaseConnectionException. 

Resolution: Ensure that you have PostgreSQL installed and set up in a manner compatible with the connection string given in AgoRapideSample.BaseController.GetDatabase.

Typically that would mean adding 

1) A user called agorapide 
2) A database called agorapide 
3) A table called p (look in AgoRapideSample log under SQL_CREATE_TABLE for the necessary autogenerated SQL-code to use)

