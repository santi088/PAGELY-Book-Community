# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: tests/e2e/reports.spec.js >> Reports tab >> loads without console errors
- Location: tests/e2e/reports.spec.js:28:3

# Error details

```
Test timeout of 30000ms exceeded.
```

```
Error: page.fill: Test timeout of 30000ms exceeded.
Call log:
  - waiting for locator('#email')

```

# Page snapshot

```yaml
- generic [active] [ref=f1e1]:
  - heading "Error response" [level=1] [ref=f1e2]
  - paragraph [ref=f1e3]: "Error code: 404"
  - paragraph [ref=f1e4]: "Message: File not found."
  - paragraph [ref=f1e5]: "Error code explanation: 404 - Nothing matches the given URI."
```

# Test source

```ts
  1   | const { test, expect } = require('@playwright/test');
  2   | 
  3   | test.describe('Reports tab', () => {
  4   |   test.beforeEach(async ({ page }) => {
  5   |     // Listen for console errors
  6   |     const errors = [];
  7   |     page.on('console', msg => {
  8   |       if (msg.type() === 'error') {
  9   |         errors.push(msg.text());
  10  |       }
  11  |     });
  12  |     page.on('pageerror', error => {
  13  |       errors.push(error.message);
  14  |     });
  15  |     page.errors = errors;
  16  |   });
  17  | 
  18  |   async function loginIfNeeded(page) {
  19  |     // Check if we're on login page
  20  |     if (page.url().includes('login.html')) {
> 21  |       await page.fill('#email', 'admin@bookcommunity.local');
      |                  ^ Error: page.fill: Test timeout of 30000ms exceeded.
  22  |       await page.fill('#password', 'admin123');
  23  |       await page.click('#loginForm button[type="submit"]');
  24  |       await page.waitForURL('**/dashboard.html', { timeout: 10000 });
  25  |     }
  26  |   }
  27  | 
  28  |   test('loads without console errors', async ({ page }) => {
  29  |     await page.goto('http://localhost:8080/pages/reports.html', { waitUntil: 'networkidle' });
  30  |     await loginIfNeeded(page);
  31  |     
  32  |     // Wait for the table to load
  33  |     await page.waitForSelector('#pageContent', { timeout: 10000 });
  34  |     
  35  |     // Check for console errors (ignore favicon and 404)
  36  |     const realErrors = page.errors.filter(e => 
  37  |       !e.includes('favicon') && 
  38  |       !e.includes('404') &&
  39  |       !e.includes('Failed to load resource')
  40  |     );
  41  |     expect(realErrors).toEqual([]);
  42  |   });
  43  | 
  44  |   test('toolbar renders with all controls', async ({ page }) => {
  45  |     await page.goto('http://localhost:8080/pages/reports.html', { waitUntil: 'networkidle' });
  46  |     await loginIfNeeded(page);
  47  |     await page.waitForSelector('#pageContent', { timeout: 10000 });
  48  |     
  49  |     // Check toolbar elements exist
  50  |     const searchInput = await page.$('.toolbar-search');
  51  |     expect(searchInput).toBeTruthy();
  52  |     
  53  |     const reasonFilter = await page.$('.toolbar-filter[data-key="reason"]');
  54  |     expect(reasonFilter).toBeTruthy();
  55  |     
  56  |     const sortSelect = await page.$('.toolbar-sort');
  57  |     expect(sortSelect).toBeTruthy();
  58  |     
  59  |     const resetBtn = await page.$('.toolbar-reset');
  60  |     expect(resetBtn).toBeTruthy();
  61  |     
  62  |     // Check reason filter options
  63  |     const options = await page.$eval('.toolbar-filter[data-key="reason"]', el => 
  64  |       Array.from(el.options).map(o => o.value)
  65  |     );
  66  |     expect(options).toContain(''); // All reasons
  67  |     expect(options).toContain('Misinformation');
  68  |     expect(options).toContain('Inappropriate Content');
  69  |     expect(options).toContain('Spam');
  70  |     
  71  |     // Check sort options
  72  |     const sortOptions = await page.$eval('.toolbar-sort', el => 
  73  |       Array.from(el.options).map(o => o.value)
  74  |     );
  75  |     expect(sortOptions).toContain('recent');
  76  |     expect(sortOptions).toContain('oldest');
  77  |     expect(sortOptions).toContain('reason');
  78  |     expect(sortOptions).toContain('reviewer');
  79  |     expect(sortOptions).toContain('book');
  80  |   });
  81  | 
  82  |   test('filter state persists after modal open/close', async ({ page }) => {
  83  |     await page.goto('http://localhost:8080/pages/reports.html', { waitUntil: 'networkidle' });
  84  |     await loginIfNeeded(page);
  85  |     await page.waitForSelector('#pageContent', { timeout: 10000 });
  86  |     
  87  |     // Apply a filter
  88  |     const reasonFilter = await page.$('.toolbar-filter[data-key="reason"]');
  89  |     await reasonFilter.selectOption('Misinformation');
  90  |     await page.waitForTimeout(500); // Wait for reload
  91  |     
  92  |     // Verify filter is applied
  93  |     const selectedValue = await page.$eval('.toolbar-filter[data-key="reason"]', el => el.value);
  94  |     expect(selectedValue).toBe('Misinformation');
  95  |     
  96  |     // Open modal (View button) if there are reports
  97  |     const viewBtn = await page.$('[data-action="viewReport"]');
  98  |     if (viewBtn) {
  99  |       await viewBtn.click();
  100 |       await page.waitForSelector('#reportModal.active', { timeout: 5000 });
  101 |       
  102 |       // Close modal
  103 |       await page.click('[data-action="closeReportModal"]');
  104 |       await page.waitForSelector('#reportModal:not(.active)', { timeout: 5000 });
  105 |       
  106 |       // Verify filter still applied
  107 |       const selectedValueAfter = await page.$eval('.toolbar-filter[data-key="reason"]', el => el.value);
  108 |       expect(selectedValueAfter).toBe('Misinformation');
  109 |     }
  110 |   });
  111 | });
```