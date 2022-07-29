FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
#COPY ["MOVGone.Bot/MOVGone.Bot.csproj", "MOVGone.Bot/"]
RUN dotnet restore "MOVGone.Bot.csproj"
#RUN dotnet build "MOVGone.Bot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MOVGone.Bot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MOVGone.Bot.dll"]
