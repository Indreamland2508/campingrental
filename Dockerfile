# 1. Khai báo môi trường chạy web (Runtime)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# 2. Khai báo môi trường Build code (SDK)
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file cấu hình và tải các thư viện về
COPY ["BAOCAOWEBNANGCAO.csproj", "./"]
RUN dotnet restore "BAOCAOWEBNANGCAO.csproj"

# Copy toàn bộ code còn lại và tiến hành Build
COPY . .
RUN dotnet build "BAOCAOWEBNANGCAO.csproj" -c Release -o /app/build

# 3. Đóng gói code (Publish) cho nhẹ
FROM build AS publish
RUN dotnet publish "BAOCAOWEBNANGCAO.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 4. Giai đoạn cuối: Copy code đã đóng gói sang môi trường chạy
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BAOCAOWEBNANGCAO.dll"]