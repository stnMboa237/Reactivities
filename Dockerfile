FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env
WORKDIR /app
EXPOSE 8080

# copy .csproj and restore distinct layers
# this cmd will copy <E:\projects\dotnetcore7react18\Reactivities\Reactivities.sln> 
# into the /app(docker container) folder
COPY "Reactivities.sln" "Reactivities.sln" 
COPY "API/API.csproj" "API/API.csproj"
COPY "Application/Application.csproj" "Application/Application.csproj"
COPY "Persistence/Persistence.csproj" "Persistence/Persistence.csproj"
COPY "Domain/Domain.csproj" "Domain/Domain.csproj"
COPY "Infrastructure/Infrastructure.csproj" "Infrastructure/Infrastructure.csproj"

# restore command will run, build and download all packages which are necessaries for the app.
RUN dotnet restore "Reactivities.sln"

# copy everything else from local folder to the WORKDIR
COPY . .

RUN dotnet publish -c Release -o out

# build a runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0.1

# now we copy the build result from the "out" directory into the "app" folder
COPY --from=build-env /app/out/ .

# then we specify the entryPoint and the startup project which is API
ENTRYPOINT [ "dotnet", "API.dll" ]