FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /source

COPY ./*.sln  ./

COPY **/*.csproj ./
RUN for file in $(ls *.csproj); do mkdir -p src/${file%.*}/ && mv $file src/${file%.*}/; done

COPY . .
RUN dotnet publish --output /app/ --configuration Release

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Quasar.Hub.dll"]
ENV ASPNETCORE_URLS http://+:5000
EXPOSE 5000
