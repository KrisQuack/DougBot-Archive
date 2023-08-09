FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["DougBot.csproj", "."]
RUN dotnet restore "DougBot.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "DougBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DougBot.csproj" -c Release -o /app/publish -r linux-x64 --self-contained true /p:PublishSingleFile=true /p:PublishTrimmed=true

FROM mcr.microsoft.com/dotnet/runtime-deps:7.0 AS final
WORKDIR /src
COPY --from=publish /app/publish .
ENTRYPOINT ["./DougBot"]
