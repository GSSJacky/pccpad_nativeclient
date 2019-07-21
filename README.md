## Native Client Demo: PCCPad under PCF with PCC and MYSQL

This is C# application which demonstrates Look-Aside caching pattern and leverages Gemfire Native Client with Pivotal Cloud Cache(PCC) and Pivotal MySQL v2 instance.

![PCCPadDemo Image](https://github.com/GSSJacky/pccpad_nativeclient/blob/master/pccpaddemo.png "PCCPadDemo Image")

## What does the service do?
This is a service that requests customer information from a CloudFoundry hosted Customer Search service and caches them in PCC. You will then see that fetching the same customer information again eliminates the expensive call to retrieve customer information from MySQL.

The Customer Search service has the following APIs:

- GET /showcache?count={amount} - get all customer info in PCC
- GET /clearcache - remove all customer info in PCC
- GET /showdb?count={amount} - get all customer info in MySQL
- GET /cleardb - remove all customer info in MySQL
- GET /loaddb?amount={amount} - load {amount} customer info into MySQL
- GET /lookasidesearch?keyword={Name} - get specific customer info by customer name and put entries into PCC
- GET /countdb - get count info from db
- GET /countcache - get count info from PCC

## Prerequisite

1.PCF2.4+, including `Pivotal Cloud Cache service`, `MySQL for Pivotal Cloud Foundry v2` service in MarketPlace.

2.Windows OS:  `Visual Studio Professional 2015` + `.Net Framework4.6.1`.




