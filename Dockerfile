# Giai đoạn 1: Runtime (Sử dụng bản Ubuntu cho ổn định hơn trên Cloud)
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
# Khử lỗi DataProtection khi chạy trên môi trường không có ổ đĩa ghi
ENV ASPNETCORE_DataProtection__AssertApplicationIdentities=false 

# Giai đoạn 2: Build (Ép về kiến trúc linux/amd64)
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0-jammy AS build
WORKDIR /src

# Copy file csproj với đường dẫn chính xác như trên GitHub của bạn
COPY ["BAOCAOWEBNANGCAO/BAOCAOWEBNANGCAO.csproj", "BAOCAOWEBNANGCAO/"]
RUN dotnet restore "BAOCAOWEBNANGCAO/BAOCAOWEBNANGCAO.csproj"

# Copy toàn bộ code
COPY . .
WORKDIR "/src/BAOCAOWEBNANGCAO"

# Build chính xác cho kiến trúc x64
RUN dotnet build "BAOCAOWEBNANGCAO.csproj" -c Release -o /app/build

# Giai đoạn 3: Publish
FROM build AS publish
RUN dotnet publish "BAOCAOWEBNANGCAO.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Giai đoạn 4: Final
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Lệnh quan trọng để tránh lỗi phân mảnh bộ nhớ trên Linux
ENV COMPlus_EnableDiagnostics=0
ENTRYPOINT ["dotnet", "BAOCAOWEBNANGCAO.dll"]