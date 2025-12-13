# ============================
# STAGE 1: BUILD (.NET 9 SDK)
# ============================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 1. Copy file csproj trước để restore
COPY MotorShop.csproj ./

# 2. Restore các package NuGet
RUN dotnet restore "MotorShop.csproj"

# 3. Copy toàn bộ source code sang image
COPY . .

# 4. Publish ra thư mục /app/publish (Release)
RUN dotnet publish "MotorShop.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ============================
# STAGE 2: RUNTIME (ASP.NET 9)
# ============================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Copy kết quả publish từ stage build
COPY --from=build /app/publish .

# Lắng nghe trên port 8080 trong container
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Chạy ứng dụng
ENTRYPOINT ["dotnet", "MotorShop.dll"]
