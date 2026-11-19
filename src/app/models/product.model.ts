export interface ProductDto {
  id: number;
  name: string;
  description?: string;
  price: number;
  stock: number;
}

export interface CreateProductDto {
  name: string;
  description?: string;
  price: number;
  stock: number;
}

export interface UpdateProductDto {
  name: string;
  description?: string;
  price: number;
  stock: number;
}
