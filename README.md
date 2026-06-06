# SMS Lite Static Demo

Static Blazor WebAssembly demo for GitHub Pages. The deployable app uses browser-side in-memory data and local storage only. It does not require `SMSLiteStaticDemo.Server`, API endpoints, databases, or external service calls.

## Deploy To GitHub Pages

1. Create an empty GitHub repository.
2. Push this repository to GitHub with `main` as the default branch.
3. In GitHub, open `Settings -> Pages`.
4. Set `Source` to `GitHub Actions`.
5. Push to `main` or run the workflow named `Deploy static Blazor app to GitHub Pages`.

The workflow publishes `SMSLiteStaticDemo/SMSLiteStaticDemo.csproj` and deploys `publish/wwwroot` to GitHub Pages over HTTPS.

## Local Static Publish

```bash
dotnet publish SMSLiteStaticDemo/SMSLiteStaticDemo.csproj -c Release
```

The static output is generated here:

```text
SMSLiteStaticDemo/bin/Release/net10.0/publish/wwwroot
```

That folder contains `index.html`, `404.html`, `.nojekyll`, `_framework`, CSS, JavaScript, and static assets.

## Local Preview

```bash
dotnet run --project SMSLiteStaticDemo/SMSLiteStaticDemo.csproj
```

Open the URL printed by the command. The app is still fully static at runtime; the dev server is only for local preview.
