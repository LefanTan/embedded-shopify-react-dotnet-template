import React from "react";
import ReactDOM from "react-dom/client";
import App from "./App.tsx";
import ExitIframe from "./ExitIframe.tsx";
import "./index.css";

import { AppProvider } from "@shopify/polaris";
import { RouterProvider, createBrowserRouter } from "react-router-dom";

const router = createBrowserRouter([
  {
    path: "/",
    element: <App />,
  },
  {
    path: "/exit-iframe",
    element: <ExitIframe />,
  },
]);

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    <AppProvider i18n={{}}>
      <RouterProvider router={router} />
    </AppProvider>
  </React.StrictMode>
);
