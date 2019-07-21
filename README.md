## Native Client Demo: PCCPad under PCF with PCC and MYSQL

This is C# application which demonstrates Look-Aside caching pattern and leverages Gemfire Native Client with Pivotal Cloud Cache(PCC) and Pivotal MySQL v2 instance.

![PCCPadDemo Image](https://github.com/GSSJacky/pccpad_nativeclient/blob/master/pccpaddemo.png "PCCPadDemo Image")


## What does the service do?
This is a service that requests customer information from a CloudFoundry hosted Customer Search service and caches them in PCC. You will then see that fetching the same customer information again from cache which will eliminates the expensive call to retrieve customer information from MySQL.

The Customer Search service has the following APIs:

- GET /showcache?count={amount} - get all customer info in PCC
- GET /clearcache - remove all customer info in PCC
- GET /showdb?count={amount} - get all customer info in MySQL
- GET /cleardb - remove all customer info in MySQL
- GET /loaddb?amount={amount} - load {amount} customer info into MySQL
- GET /lookasidesearch?keyword={Name} - get specific customer info by customer name and put entries into PCC
- GET /countdb - get count info from db
- GET /countcache - get count info from PCC

![PCCPadDemo Architecture Image](https://github.com/GSSJacky/pccpad_nativeclient/blob/master/PCCPadDemoArchitecture.png "PCCPad Demo Architecture Image")

## Prerequisite

1.PCF2.4+ env:
- `Pivotal Cloud Cache service`
- `MySQL for Pivotal Cloud Foundry v2` service
- `Pivotal Application Service for Windows(PASW)` with windows2016 stack and "binary buildpack or HWC buildpack"

2.Windows OS:
- Visual Studio Professional 2015
- .Net Framework4.6.1 
- cf cli (version 6.43.0+815ea2f3d.2019-02-20)
- Pivotal Gemfire Native Client 10.0.0.2 
(http://gemfire-native.docs.pivotal.io/100/gemfire-native-client/install-upgrade-native.html)

3.This project is using the below packages:

```
<packages>
  <package id="BouncyCastle" version="1.8.3.1" targetFramework="net461" />
  <package id="Faker.Net" version="1.1.1" targetFramework="net461" />
  <package id="Google.Protobuf" version="3.6.1" targetFramework="net461" />
  <package id="JsonPath" version="1.0.5" targetFramework="net461" />
  <package id="Microsoft.AspNet.WebApi.Client" version="5.2.7" targetFramework="net461" />
  <package id="Microsoft.AspNet.WebApi.Core" version="5.2.7" targetFramework="net461" />
  <package id="Microsoft.AspNet.WebApi.Owin" version="5.2.7" targetFramework="net461" />
  <package id="Microsoft.AspNet.WebApi.OwinSelfHost" version="5.2.7" targetFramework="net461" />
  <package id="Microsoft.Owin" version="4.0.1" targetFramework="net461" />
  <package id="Microsoft.Owin.Diagnostics" version="4.0.1" targetFramework="net461" />
  <package id="Microsoft.Owin.FileSystems" version="4.0.1" targetFramework="net461" />
  <package id="Microsoft.Owin.Host.HttpListener" version="4.0.1" targetFramework="net461" />
  <package id="Microsoft.Owin.Hosting" version="4.0.1" targetFramework="net461" />
  <package id="Microsoft.Owin.SelfHost" version="4.0.1" targetFramework="net461" />
  <package id="Microsoft.Owin.StaticFiles" version="4.0.1" targetFramework="net461" />
  <package id="MySql.Data" version="8.0.16" targetFramework="net461" />
  <package id="NBuilder" version="6.0.0" targetFramework="net461" />
  <package id="Newtonsoft.Json" version="9.0.1" targetFramework="net461" />
  <package id="Owin" version="1.0" targetFramework="net461" />
</packages>
```

## How to run this demo

Step1:
Download this project to a local env and then unzip it into windows env which has installed vistual studio professional 2015.

Step2:
- Open pccpad.sln with visual studio professional 2015.
- Add [pivotal-gemfire-native1002_HomePath]\bin\Pivotal.GemFire.dll to project's reference.
- Compile this project by running `build`-->`build solution` from visual studio menu. It will generalte pccpad.exe under `pccpad_nativeclient\pccpad\bin\x64\Release` folder.

Step3:

