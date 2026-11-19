import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProductDto, CreateProductDto, UpdateProductDto } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private http = inject(HttpClient);
  private apiUrl = '/api/products'; // Routed through proxy.conf.json to .NET API

  getProducts(): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>(this.apiUrl);
  }

  getProductById(id: number): Observable<ProductDto> {
    return this.http.get<ProductDto>(`${this.apiUrl}/${id}`);
  }

  createProduct(dto: CreateProductDto): Observable<ProductDto> {
    return this.http.post<ProductDto>(this.apiUrl, dto);
  }

  updateProduct(id: number, dto: UpdateProductDto): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/${id}`, dto);
  }

  deleteProduct(id: number): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }

  addToStock(id: number, quantity: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/add-to-stock/${quantity}`, {});
  }

  decrementStock(id: number, quantity: number): Observable<void> {
    return this.http.post<void>(`${this.apiUrl}/${id}/decrement-stock/${quantity}`, {});
  }

  searchProducts(name: string): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>(`${this.apiUrl}/search?name=${encodeURIComponent(name)}`);
  }

  getStockLevel(min: number, max: number): Observable<ProductDto[]> {
    return this.http.get<ProductDto[]>(`${this.apiUrl}/stock-level?min=${min}&max=${max}`);
  }
}