CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE INDEX idx_product_name_trgm ON "Products" USING GIN ("Name" gin_trgm_ops);
CREATE INDEX idx_product_sales_product_id ON "ProductSales" ("ProductId");
