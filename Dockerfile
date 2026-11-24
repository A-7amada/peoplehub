FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY PeopleHub.sln ./
COPY src/PeopleHub.Web/PeopleHub.Web.csproj src/PeopleHub.Web/
COPY src/PeopleHub.Application/PeopleHub.Application.csproj src/PeopleHub.Application/
COPY src/PeopleHub.Domain/PeopleHub.Domain.csproj src/PeopleHub.Domain/
COPY src/PeopleHub.Infrastructure/PeopleHub.Infrastructure.csproj src/PeopleHub.Infrastructure/
RUN dotnet restore src/PeopleHub.Web/PeopleHub.Web.csproj
COPY src/ src/
RUN dotnet publish src/PeopleHub.Web/PeopleHub.Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PeopleHub.Web.dll"]
