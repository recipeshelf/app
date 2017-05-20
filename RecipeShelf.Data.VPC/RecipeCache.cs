﻿using RecipeShelf.Data.VPC.Models;
using RecipeShelf.Data.VPC.Proxies;
using RecipeShelf.Common;
using RecipeShelf.Common.Models;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace RecipeShelf.Data.VPC
{
    public sealed class RecipeCache : Cache
    {
        public override string Table => "Recipes";

        private IngredientCache _ingredientCache;

        protected override string NamesKey => KeyRegistry.Recipes.Names;

        protected override string SearchWordsKey => KeyRegistry.Recipes.SearchWords;

        protected override string LocksKey => KeyRegistry.Recipes.Locks;

        public RecipeCache(ICacheProxy cacheProxy, IngredientCache ingredientCache, ILogger<RecipeCache> logger) : base(cacheProxy, logger)
        {
            _ingredientCache = ingredientCache;
        }

        public string[] ByChef(string chefId) => CacheProxy.Members(KeyRegistry.Recipes.ChefId.Append(chefId));

        public string[] ByFilter(RecipeFilter filter)
        {
            var keys = new List<string>();
            if (filter.Vegan != null) keys.Add(KeyRegistry.Recipes.Vegan.Append(filter.Vegan.Value));
            if (filter.OvernightPreparation != null) keys.Add(KeyRegistry.Recipes.OvernightPreparation.Append(filter.OvernightPreparation.Value));
            if (filter.IngredientIds != null && filter.IngredientIds.Length > 0) keys.Add(CacheProxy.Combine(new CombineOptions(LogicalOperator.Or, KeyRegistry.Recipes.IngredientId, filter.IngredientIds)));
            if (filter.Regions != null && filter.Regions.Length > 0) keys.Add(CacheProxy.Combine(new CombineOptions(LogicalOperator.Or, KeyRegistry.Recipes.Region, filter.Regions)));
            if (filter.Cuisines != null && filter.Cuisines.Length > 0) keys.Add(CacheProxy.Combine(new CombineOptions(LogicalOperator.Or, KeyRegistry.Recipes.Cuisine, filter.Cuisines)));
            if (filter.SpiceLevels != null && filter.SpiceLevels.Length > 0) keys.Add(CacheProxy.Combine(new CombineOptions(LogicalOperator.Or, KeyRegistry.Recipes.SpiceLevel, filter.SpiceLevels.ToStrings())));
            if (filter.TotalTimes != null && filter.TotalTimes.Length > 0) keys.Add(CacheProxy.Combine(new CombineOptions(LogicalOperator.Or, KeyRegistry.Recipes.TotalTime, filter.TotalTimes.ToStrings())));
            if (filter.Collections != null && filter.Collections.Length > 0) keys.Add(CacheProxy.Combine(new CombineOptions(LogicalOperator.Or, KeyRegistry.Recipes.Collection, filter.Collections)));
            if (keys.Count == 0) return new string[0];
            return CacheProxy.Members(CacheProxy.Combine(new CombineOptions(LogicalOperator.And, keys.ToArray())));
        }

        public override bool IsVegan(string id) => CacheProxy.IsMember(KeyRegistry.Recipes.Vegan.Append(true), id);

        public string[] GetChefs() => CacheProxy.Members(KeyRegistry.Recipes.ChefId);

        public string[] GetCollections() => CacheProxy.Members(KeyRegistry.Recipes.Collection);

        public string[] GetCuisines() => CacheProxy.Members(KeyRegistry.Recipes.Cuisine);

        public string[] GetRegions() => CacheProxy.Members(KeyRegistry.Recipes.Region);

        public long GetCountForCollection(string collection)
        {
            return CacheProxy.Count(KeyRegistry.Recipes.Collection.Append(collection));
        }

        public void Store(Recipe recipe)
        {
            Logger.LogDebug("Saving Recipe {Id} in cache", recipe.Id);

            var oldNames = CacheProxy.Get(KeyRegistry.Recipes.Names, recipe.Id);

            var vegan = true;   // Store if recipe is vegan
            foreach (var ingredientId in recipe.IngredientIds)
            {
                if (_ingredientCache.IsVegan(ingredientId)) continue;
                vegan = false;
                break;
            }

            var batch = new List<IEntry>
            {
                new HashEntry(KeyRegistry.Recipes.Names, recipe.Id, string.Join(Environment.NewLine, recipe.Names)),
                // Store list of ingredientIds with recipeId as key
                new HashEntry(KeyRegistry.Ingredients.RecipeId, recipe.Id, string.Join(",", recipe.IngredientIds)),
                new SetEntry(KeyRegistry.Recipes.Vegan, vegan, recipe.Id),
                new SetEntry(KeyRegistry.Recipes.IngredientId, recipe.IngredientIds, recipe.Id),
                new SetEntry(KeyRegistry.Recipes.OvernightPreparation, recipe.OvernightPreparation, recipe.Id),
                new SetEntry(KeyRegistry.Recipes.Region, recipe.Region, recipe.Id),
                new SetEntry(KeyRegistry.Recipes.Cuisine, recipe.Cuisine, recipe.Id),
                new SetEntry(KeyRegistry.Recipes.SpiceLevel, recipe.SpiceLevel.ToString(), recipe.Id),
                new SetEntry(KeyRegistry.Recipes.TotalTime, recipe.TotalTime.ToString(), recipe.Id),
                new SetEntry(KeyRegistry.Recipes.Collection, recipe.Collections, recipe.Id)
            };
            batch.AddRange(CreateSearchWordEntries(recipe.Id, oldNames, recipe.Names));

            CacheProxy.Store(batch);
        }

        public void Remove(string id)
        {
            Logger.LogDebug("Removing Recipe {Id} from cache", id);

            var oldNames = CacheProxy.Get(KeyRegistry.Recipes.Names, id);

            var batch = new List<IEntry>
            {
                new HashEntry(KeyRegistry.Recipes.Names, id),
                new SetEntry(KeyRegistry.Recipes.Vegan, id),
                new SetEntry(KeyRegistry.Recipes.IngredientId, id),
                new SetEntry(KeyRegistry.Recipes.OvernightPreparation, id),
                new SetEntry(KeyRegistry.Recipes.Region, id),
                new SetEntry(KeyRegistry.Recipes.Cuisine, id),
                new SetEntry(KeyRegistry.Recipes.SpiceLevel, id),
                new SetEntry(KeyRegistry.Recipes.TotalTime, id),
                new SetEntry(KeyRegistry.Recipes.Collection, id)
            };
            batch.AddRange(CreateSearchWordEntries(id, oldNames, new string[0]));

            CacheProxy.Store(batch);
        }
    }
}