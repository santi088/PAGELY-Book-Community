const { test, expect } = require('@playwright/test');

test.describe('Reports tab', () => {
  test.beforeEach(async ({ page }) => {
    // Listen for console errors
    const errors = [];
    page.on('console', msg => {
      if (msg.type() === 'error') {
        errors.push(msg.text());
      }
    });
    page.on('pageerror', error => {
      errors.push(error.message);
    });
    page.errors = errors;
  });

  async function loginIfNeeded(page) {
    // Check if we're on login page
    if (page.url().includes('login.html')) {
      await page.fill('#email', 'admin@bookcommunity.local');
      await page.fill('#password', 'admin123');
      await page.click('#loginForm button[type="submit"]');
      await page.waitForURL('**/dashboard.html', { timeout: 10000 });
    }
  }

  test('loads without console errors', async ({ page }) => {
    await page.goto('http://localhost:8080/pages/reports.html', { waitUntil: 'networkidle' });
    await loginIfNeeded(page);
    
    // Wait for the table to load
    await page.waitForSelector('#pageContent', { timeout: 10000 });
    
    // Check for console errors (ignore favicon and 404)
    const realErrors = page.errors.filter(e => 
      !e.includes('favicon') && 
      !e.includes('404') &&
      !e.includes('Failed to load resource')
    );
    expect(realErrors).toEqual([]);
  });

  test('toolbar renders with all controls', async ({ page }) => {
    await page.goto('http://localhost:8080/pages/reports.html', { waitUntil: 'networkidle' });
    await loginIfNeeded(page);
    await page.waitForSelector('#pageContent', { timeout: 10000 });
    
    // Check toolbar elements exist
    const searchInput = await page.$('.toolbar-search');
    expect(searchInput).toBeTruthy();
    
    const reasonFilter = await page.$('.toolbar-filter[data-key="reason"]');
    expect(reasonFilter).toBeTruthy();
    
    const sortSelect = await page.$('.toolbar-sort');
    expect(sortSelect).toBeTruthy();
    
    const resetBtn = await page.$('.toolbar-reset');
    expect(resetBtn).toBeTruthy();
    
    // Check reason filter options
    const options = await page.$eval('.toolbar-filter[data-key="reason"]', el => 
      Array.from(el.options).map(o => o.value)
    );
    expect(options).toContain(''); // All reasons
    expect(options).toContain('Misinformation');
    expect(options).toContain('Inappropriate Content');
    expect(options).toContain('Spam');
    
    // Check sort options
    const sortOptions = await page.$eval('.toolbar-sort', el => 
      Array.from(el.options).map(o => o.value)
    );
    expect(sortOptions).toContain('recent');
    expect(sortOptions).toContain('oldest');
    expect(sortOptions).toContain('reason');
    expect(sortOptions).toContain('reviewer');
    expect(sortOptions).toContain('book');
  });

  test('filter state persists after modal open/close', async ({ page }) => {
    await page.goto('http://localhost:8080/pages/reports.html', { waitUntil: 'networkidle' });
    await loginIfNeeded(page);
    await page.waitForSelector('#pageContent', { timeout: 10000 });
    
    // Apply a filter
    const reasonFilter = await page.$('.toolbar-filter[data-key="reason"]');
    await reasonFilter.selectOption('Misinformation');
    await page.waitForTimeout(500); // Wait for reload
    
    // Verify filter is applied
    const selectedValue = await page.$eval('.toolbar-filter[data-key="reason"]', el => el.value);
    expect(selectedValue).toBe('Misinformation');
    
    // Open modal (View button) if there are reports
    const viewBtn = await page.$('[data-action="viewReport"]');
    if (viewBtn) {
      await viewBtn.click();
      await page.waitForSelector('#reportModal.active', { timeout: 5000 });
      
      // Close modal
      await page.click('[data-action="closeReportModal"]');
      await page.waitForSelector('#reportModal:not(.active)', { timeout: 5000 });
      
      // Verify filter still applied
      const selectedValueAfter = await page.$eval('.toolbar-filter[data-key="reason"]', el => el.value);
      expect(selectedValueAfter).toBe('Misinformation');
    }
  });
});