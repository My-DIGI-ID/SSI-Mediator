FROM ubuntu:18.04 AS base

RUN apt-get update
RUN apt-get install -y wget apt-transport-https software-properties-common

RUN apt-key adv --keyserver keyserver.ubuntu.com --recv-keys CE7709D068DB5E88
RUN add-apt-repository "deb https://repo.sovrin.org/sdk/deb bionic stable"
RUN apt-get update
RUN apt-get install -y libindy=1.15.0-bionic

RUN wget -q https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb
RUN dpkg -i packages-microsoft-prod.deb
RUN apt-get update
RUN apt-get install -y dotnet-sdk-5.0

RUN echo "DOTNET_CLI_TELEMETRY_OPTOUT=1" | tee -a /etc/environment
RUN export DOTNET_CLI_TELEMETRY_OPTOUT=1

WORKDIR /app
EXPOSE 80
EXPOSE 443
EXPOSE 7000

WORKDIR /src
COPY ["SSIMediator/SSIMediator.csproj", "SSIMediator/"]

RUN dotnet restore "SSIMediator/SSIMediator.csproj" -s "https://api.nuget.org/v3/index.json" -s "https://www.myget.org/F/agent-framework/api/v3/index.json"
COPY . .
WORKDIR "/src/SSIMediator"

RUN dotnet build "SSIMediator.csproj" -c Debug -o /app -r ubuntu.18.04-x64

FROM base AS publish

RUN dotnet publish "SSIMediator.csproj" -c Debug -o /app -r ubuntu.18.04-x64 --self-contained

FROM base AS final
WORKDIR /app

COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SSIMediator.dll"]
