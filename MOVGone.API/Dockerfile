FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
RUN apk add --no-cache ffmpeg && ln -s /usr/bin/ffmpeg /app/ffmpeg; 

FROM mcr.microsoft.com/dotnet/sdk:6.0-alpine AS build
WORKDIR /src
COPY . .
RUN dotnet restore "MOVGone.API.csproj"
#RUN dotnet build "MOVGone.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MOVGone.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MOVGone.API.dll"]
