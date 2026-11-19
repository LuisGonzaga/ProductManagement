import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProductService } from '../../services/product.service';
import { ProductDto } from '../../models/product.model';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './product-list.html',
  styleUrls: ['./product-list.css'],
})
export class ProductListComponent implements OnInit {
  private productService = inject(ProductService);

  products = signal<ProductDto[]>([]);
  searchQuery = '';
  loading = signal(false);
  errorMessage = signal('');

  newProduct = {
    name: '',
    description: '',
    price: 0,
    stock: 0,
  };

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.loading.set(true);
    this.errorMessage.set('');
    this.productService.getProducts().subscribe({
      next: (data) => {
        if (!Array.isArray(data) || data.length === 0) {
          this.products.set([]);
          this.errorMessage.set('No response or no products returned from the API.');
          this.loading.set(false);
          return;
        }

        this.products.set(data);
        this.loading.set(false);
      },
      error: (err) => {
        this.products.set([]);
        this.errorMessage.set(
          'Failed to load products from the API. Check if the backend is running.',
        );
        this.loading.set(false);
        console.error(err);
      },
    });
  }

  onSearch(): void {
    if (!this.searchQuery.trim()) {
      this.loadProducts();
      return;
    }
    this.productService.searchProducts(this.searchQuery).subscribe({
      next: (data) => this.products.set(data),
      error: (err) => console.error('Search failed', err),
    });
  }

  addProduct(): void {
    if (!this.newProduct.name.trim() || this.newProduct.price < 0) {
      alert('Please enter a valid product name and price.');
      return;
    }

    this.productService.createProduct(this.newProduct).subscribe({
      next: (createdProduct) => {
        // Append the newly created product to the existing signal array
        this.products.update((currentList) => [...currentList, createdProduct]);

        // Reset the form fields
        this.newProduct = { name: '', description: '', price: 0, stock: 0 };
      },
      error: (err) => {
        alert(err.error?.detail || 'Failed to create product.');
        console.error(err);
      },
    });
  }

  adjustStock(id: number, quantity: number, isAdd: boolean): void {
    const request$ = isAdd
      ? this.productService.addToStock(id, quantity)
      : this.productService.decrementStock(id, quantity);

    request$.subscribe({
      next: () => this.loadProducts(),
      error: (err) => {
        alert(err.error?.detail || 'Stock adjustment failed.');
        console.error(err);
      },
    });
  }

  deleteProduct(id: number): void {
    if (confirm(`Are you sure you want to delete product #${id}?`)) {
      this.productService.deleteProduct(id).subscribe({
        next: () => this.loadProducts(),
        error: (err) => alert('Failed to delete product.'),
      });
    }
  }
}
