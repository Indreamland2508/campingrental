# 1. Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 2. SDK để Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# SỬA ĐƯỜNG DẪN Ở ĐÂY: Trỏ vào thư mục chứa file .csproj
COPY ["BAOCAOWEBNANGCAO/BAOCAOWEBNANGCAO.csproj", "BAOCAOWEBNANGCAO/"]
RUN dotnet restore "BAOCAOWEBNANGCAO/BAOCAOWEBNANGCAO.csproj"

# Copy toàn bộ và build
COPY . .
WORKDIR "/src/BAOCAOWEBNANGCAO"
RUN dotnet build "BAOCAOWEBNANGCAO.csproj" -c Release -o /app/build

# 3. Publish
FROM build AS publish
RUN dotnet publish "BAOCAOWEBNANGCAO.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BAOCAOWEBNANGCAO.dll"]