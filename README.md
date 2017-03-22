# AgoRapideSample

Lightweight small framework for getting started with using AgoRapide. 

NOTE: AgoRapideSample has not been created yet. 

For the time being AgoRapideSample is included in the source code for the AgoRapide-library itself. In order to sample AgoRapide you should therefore clone the AgoRapide repository itself to your computer.

TODO: Move AgoRapideSample into a separate repository (this repository) and link to AgoRapide through Nuget. 
TODO: In other words, create a Nuget package for AgoRapide first.

FAQ:

## IIS / IISExpress returns "403 - Forbidden: Access is denied"

IIS returns "403 - Forbidden: Access is denied" response when starting the AgoRapideSample application.

Most probably something went wrong quite early within the Startup.Configuration-method. 

Resolution: Check the log path for any exception information. 

Note that the log path defaults to @"c:\p\Logfiles\AgoRapideSample\AgoRapideLog_[DATE_HOUR].txt".

The following problems are common:

### Startup.GetEnvironment throws an UnknownEnvironmentException. 

Resolution: Adjust the code within Startup.GetEnvironment.

### PostgreSQLDatabase throws an OpenDatabaseConnectionException. 

Resolution: Ensure that you have PostgreSQL installed and set up compatible with the connection string given in AgoRapideSample.BaseController.GetDatabase.

Typically that would mean adding 

1) A user called AgoRapide 
2) A database called AgoRapide 
3) A table called p (look in AgoRapideSample log under SQL_CREATE_TABLE for the necessary autogenerated SQL-code to use)

