var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume("FreelancePlatform_PostgresData")
    .WithPgAdmin();

var freelanceDb = postgres.AddDatabase("freelancedb");

var api = builder.AddProject<Projects.FreelancePlatform_Api>("api")
    .WithReference(freelanceDb)
    .WaitFor(freelanceDb);

builder.Build().Run();