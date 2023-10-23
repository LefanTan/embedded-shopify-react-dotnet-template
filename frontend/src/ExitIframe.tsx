import { useEffect } from "react";
import { useLocation } from "react-router-dom";

export default function ExitIframe() {
  const { search } = useLocation();

  useEffect(() => {
    (async function () {
      const params = new URLSearchParams(search);
      const redirectUri = params.get("redirectUri");

      if (!redirectUri) {
        return;
      }

      const url = new URL(decodeURIComponent(redirectUri));

      if (
        [location.hostname, "admin.shopify.com"].includes(url.hostname) ||
        url.hostname.endsWith(".myshopify.com")
      ) {
        console.log("Redirecting to", redirectUri);

        try {
          // Redirect to the redirectUri
          // Polaris will automatically redirect to outside the iframe
          open(redirectUri, "_top");
        } catch (e) {
          console.log(e);
        }
      }
    })();
  }, [search]);

  return <div>Redirecting...</div>;
}
