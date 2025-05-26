# Serverless API

This is a serverless API I made for Azure Functions with features to make it secure & scalable for production-use. Connected to an MSSQL Database with Entity Framework & built with a repository pattern for easy maintainability. It uses the modern .net 9 isolated worker model as opposed to the in-process model which will be deprecated in 2026.

---

## 💰 Why use Serverless?
Azure Functions offers scale-to-zero billing and top-notch PAYG pricing which means minimal cloud expenses both for small and high workloads. Azure Functions is often used in microservices and internal app communication rather than as a customer-facing API yet it still remains the most cost-effective way of launching a public API with high scalability compared to traditional API services such as App Service. There is increased complexity during the coding process compared to ASP.NET Web APIs as it is less well-documented and needs a bit more work on registering middleware but the money aspect makes up for this.
<br><br>
With the free quota on the consumption plan, the api can run 24/7 for free with monthly api requests below 1 million.
Beyond that, each 1 million requests costs 0.2$ - so 50 million API requests per month would cost you 10$ plus the Duration(GB-s) cost, but if you don't do heavy calculations this is also a very small cost.
<br><br>
The biggest negative with Azure Functions that is usually brought up are cold starts, which is a serverless consequence of having to boot up after being idle. This can be mitigated by paying for an always-on instance, but can also be avoided for completely free by triggering a warm-up function once every 5 minutes to counteract the server going idle. Doing this 24/7 would be 8,640 requests/month (which is inconsequential for your pricing).

---

## ✅ Features

- Basic Account Endpoints (Register,Login,Logout)
- Block fraudulent sign-ups
  * Verification email sent on signup
  * Dynamically updated blocklist of disposable email domains
  * Logged IPs of previous free-trial accounts
- IP Rate-limiting
- Good Security Practices
  * JWT Auth w/ http-only cookies
  * BCrypt password hashing w/ per-user salts

---

## 🧰 Technologies Used

- **.NET 9 Isolated Worker Model**
- **Entity Framework Core**
- **SQL Server**
