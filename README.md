# React (Polaris) + .NET 7 Template for Embedded Shopify App

This is a basic template for Embedded Shopify App built with React and .NET for this [article](https://medium.com/@lefantan/creating-an-embedded-shopify-app-with-react-and-net-7-9adb96bd303f).

## Tech Stack

- [.NET 7](https://dotnet.microsoft.com/en-us/apps/aspnet/apis) builds the backend.
  - SQLite as the database solution
- [Vite](https://vitejs.dev/) builds the [React](https://reactjs.org/) frontend.
- [React Router](https://reactrouter.com/) is used for routing. We wrap this with file-based routing.

The following Shopify tools complement these third-party tools to ease app development:

- [Shopify Sharp](https://github.com/nozzlegear/ShopifySharp) is a community-supported library to help with authentication and making API calls to Shopify.
- [Polaris React](https://polaris.shopify.com/) is a powerful design system and component library that helps developers build high quality, consistent experiences for Shopify merchants.

## Local Development

### Frontend

using npm

```
npm install && npm run dev
```

During build, you'll need to populate `VITE_SHOPIFY_ID` in `.env` with the Shopify Client Id of your app in order for App Bridge to work.

```
Read me in progress...
```
