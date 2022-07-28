FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["NoMOV.Bot/NoMOV.Bot.csproj", "NoMOV.Bot/"]
RUN dotnet restore "NoMOV.Bot/NoMOV.Bot.csproj"
COPY . .
WORKDIR "/src/NoMOV.Bot"
RUN dotnet build "NoMOV.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "NoMOV.Bot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "NoMOV.Bot.dll"]
