import {
  BlockStack,
  Card,
  OptionList,
  Page,
  Spinner,
  Text,
} from "@shopify/polaris";
import "@shopify/polaris/build/esm/styles.css";
import { useEffect, useState } from "react";

type Product = {
  title: string;
  id: number;
  images: {
    src: string;
    alt: string;
  }[];
};

function App() {
  const [products, setProducts] = useState<Product[]>([]);
  const [selected, setSelected] = useState<string[]>([]);

  useEffect(() => {
    fetch("/api/products")
      .then((response) => response.json())
      .then((data) => {
        setProducts(data.items);
      });
  }, []);

  return (
    <Page title="Demo Products">
      <Card>
        <Text variant="headingMd" as="h2">
          Select a Product
        </Text>
        <BlockStack>
          {products.length === 0 ? (
            <Spinner accessibilityLabel="Spinner example" size="large" />
          ) : (
            <OptionList
              options={products.map((p) => ({
                value: p.id.toString(),
                label: p.title,
              }))}
              onChange={setSelected}
              selected={selected}
            ></OptionList>
          )}
        </BlockStack>
      </Card>
    </Page>
  );
}

export default App;
