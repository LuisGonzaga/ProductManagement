# Product Management Dashboard (Angular Frontend)

A modern, responsive, CRUD-enabled product management dashboard built with **Angular (Standalone Components & Signals)**, communicating with a **.NET 10 Minimal API** backend.

---

## Tech Stack & Architecture

- **Framework:** Angular 18+ (Standalone Components architecture)
- **Dependency Injection:** Modern `inject()` function pattern
- **State & Reactivity:** Angular Signals (`signal`, `set`, `update`) paired with RxJS Observables
- **Forms & Binding:** Template-driven forms utilizing `FormsModule` and `[(ngModel)]`
- **Styling:** Custom wide-screen optimized CSS card layout and responsive data tables
- **Communication:** Angular `HttpClient` interacting with RESTful endpoints

---

## Getting Started

### Prerequisites

- **Node.js:** v26.13.1
- **npm:** 11.20.0
- **Visual Studio Code:** v1.146.0
- **Angular CLI**
- **Backend:** .NET 10 backend running locally on `http://localhost:5150`

### Installation & Execution

1. Clone the repository and navigate to the frontend directory.

2. Install dependencies:

   ```bash
   npm install
   ```

3. Run the development server:

   ```bash
   npm start
   ```

4. Open your browser and navigate to `http://localhost:4200`.

---

## Technical Details

- **Service-Driven API Layer:** All HTTP communication is abstracted into `ProductService`, managing endpoints for fetching products, searching by name, adjusting stock quantities (`+1` / `-1`), creating new items, and deleting records.

- **Signal-Based Reactivity (`ProductListComponent`):**
  - Component state—including the product list (`products`), loading indicators (`loading`), and error states (`errorMessage`)—is managed entirely using Angular Signals for fine-grained reactivity.
  - State mutations use `.set()` for API reloads and `.update()` with immutable array spreading (`[...currentList, createdProduct]`) to instantly append newly created products to the UI.

---

## Challenges & Solutions Encountered

### 1. Immediate UI State Synchronization on Product Creation

- **The Problem:** When adding a new product, triggering a full network reload (`loadProducts()`) for every minor mutation introduces unnecessary HTTP traffic and can cause UI flickering.

- **The Solution:** Leveraged Angular Signals' `.update()` method combined with immutable array spreading (`[...currentList, createdProduct]`). This instantly appends the newly created record to the local signal state the moment the API returns successfully, keeping the UI instantly synchronized without redundant fetches.

### 2. Wide-Screen Form Layout & Field Responsiveness

- **The Problem:** Standard inline form inputs for adding products tended to look squished or wrap awkwardly on varying display sizes, leaving input fields cramped next to action buttons.

- **The Solution:** Designed a flexible form grid utilizing `flex-wrap` and flexible basis sizing (`flex: 1 1 200px`) inside a structured card container. This ensures form inputs scale smoothly, expanding gracefully on wide monitors while remaining fully usable on standard displays.
