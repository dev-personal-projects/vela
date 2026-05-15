# Finance Tracker REST API

Production REST routes are versioned under `/api/v1`.

## Public

| Method | Route | Purpose |
| --- | --- | --- |
| `GET` | `/api/v1/health` | Function App health check |
| `POST` | `/api/v1/auth/signup` | Create an account |
| `POST` | `/api/v1/auth/signin` | Sign in and receive JWT tokens |

## Protected

Send:

```http
Authorization: Bearer <accessToken>
```

| Method | Route |
| --- | --- |
| `GET` | `/api/v1/incomes?fromDate=&toDate=&source=` |
| `GET` | `/api/v1/incomes/{id}` |
| `POST` | `/api/v1/incomes` |
| `PUT` | `/api/v1/incomes/{id}` |
| `DELETE` | `/api/v1/incomes/{id}` |
| `GET` | `/api/v1/expenses?fromDate=&toDate=&category=` |
| `GET` | `/api/v1/expenses/{id}` |
| `POST` | `/api/v1/expenses` |
| `PUT` | `/api/v1/expenses/{id}` |
| `DELETE` | `/api/v1/expenses/{id}` |
| `GET` | `/api/v1/savings-goals` |
| `GET` | `/api/v1/savings-goals/{id}` |
| `POST` | `/api/v1/savings-goals` |
| `PUT` | `/api/v1/savings-goals/{id}` |
| `POST` | `/api/v1/savings-goals/{id}/contributions` |
| `PATCH` | `/api/v1/savings-goals/{id}/progress` |
| `DELETE` | `/api/v1/savings-goals/{id}` |
| `GET` | `/api/v1/loans?type=&status=` |
| `GET` | `/api/v1/loans/{id}` |
| `POST` | `/api/v1/loans` |
| `PUT` | `/api/v1/loans/{id}` |
| `GET` | `/api/v1/loans/{id}/repayments` |
| `POST` | `/api/v1/loans/{id}/repayments` |
| `POST` | `/api/v1/loans/{id}/settle` |
| `DELETE` | `/api/v1/loans/{id}` |
| `GET` | `/api/v1/dashboard?fromDate=&toDate=&activityLimit=` |

GraphQL has been moved out of the production branch and preserved on `graphql-learning`.
