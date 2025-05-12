# Serverless API

This is a serverless API for Azure Functions with features to make it secure & scalable for production-use. Connected to an MSSQL Database with Entity Framework & built with a repository pattern for easy maintainability.
<br><br>
I built this as a boilerplate for my future personal projects so there are a few design liberties (no interfaces because no unit testing or >2 implementations). Also, everything is referring to <a href="https://batproxy.com">Batproxy</a>, because that is the name of the current project I am working on.

---

## 💰 Why use Serverless?
Azure Functions offers scale-to-zero billing and top-notch PAYG pricing which means minimal cloud expenses both for small and high workloads compared to traditional API deployment such as App Service, Container Apps and VMs. It is great for businesses that value both their money and scalability.
<br><br>
With the free quota on the consumption plan, you can run your api 24/7 for free as long as the monthly api requests stay below 1 million.
Beyond that each million requests costs 0.2$ - so 50 million API requests per month would cost you 10$. There is of course the Duration(GB-s) cost but if you don't do heavy calculations this is also an incredibly small cost.
<br><br>
The biggest negative with Azure Functions that is usually brought up are cold starts, which is a serverless consequence of having to boot up after being idle. This can be mitigated with an always-on instance, but can also be avoided for free by triggering warm-up functions once every 5 minutes to counteract the server going idle. Doing this 24/7 would be 8,640 requests/month which is inconsequential with the free quota & PAYG pricing.

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
