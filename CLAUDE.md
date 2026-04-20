## Instructions about endpoints
Each endpoints has its own C# class. All classes located in `src/LogSystem.WebApp/Endpoints`.
Example in `src/LogSystem.WebApp/Endpoints/CreateOrUpdateLogCollectionEndpoint.cs`
Request and response classes are created only if necessary.
All parameters from request must be nullable.
When creating new endpoint, always write code based on a pre-existing endpoint.