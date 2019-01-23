	private ISynchronizedCacheManager cacheManager;
  

public async Task<List<IShoppingListGroup>> GenerateShoppingListAsync(CultureInfo culture, IList<IRecipeAmount> itemAmounts) {

			string key = "";
			foreach(var recipeAmount in itemAmounts) {
				key += $"{recipeAmount.Id}-{recipeAmount.Amount}";
			}
			
			var cacheKey = MakeCacheKey($"GenerateShoppingListAsync:{key}:{culture}");
			return await cacheManager.GetAsync(cacheKey, () => shoppingListService.GenerateShoppingListAsync(culture, itemAmounts), _cacheTimeout, mutex);
		}
