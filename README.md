# TaskBridge API

ASP.NET Core Web API for bridging tasks (projects) and notifications.

## Layout

```
.github/                    Copilot instructions for this repo
src/
  projects/                 Project/task management models + controller
  notifications/            Notification models + controller
tests/
  TaskBridge-API.Tests/     xUnit test project
Controllers/                Sample WeatherForecast controller (template default)
```

## Running

```bash
dotnet run
```

Swagger UI is available at `/swagger` when running in Development.

## Testing

```bash
dotnet test
```
