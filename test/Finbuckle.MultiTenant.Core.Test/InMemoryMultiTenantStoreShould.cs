//    Copyright 2018 Andrew White
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Stores;
using Xunit;

public class InMemoryMultiTenantStoreShould
{
    private static InMemoryMultiTenantStore CreateTestStore(bool ignoreCase = true)
    {
        var store = new InMemoryMultiTenantStore(ignoreCase);
        store.TryAddAsync(new TenantContext("initech", "initech", "Initech", null, null, null));
        store.TryAddAsync(new TenantContext("lol", "lol", "Lol, Inc.", null, null, null));

        return store;
    }

    [Fact]
    public void GetTenantFromStore()
    {
        var store = CreateTestStore();
        Assert.Equal("initech", store.GetByIdentifierAsync("initech").Result.Identifier);
    }

    [Fact]
    public void GetTenantFromStoreIgnoringCaseByDefault()
    {
        var store = CreateTestStore();
        Assert.Equal("initech", store.GetByIdentifierAsync("iNitEch").Result.Identifier);
    }

    [Fact]
    public void GetTenantFromStoreMatchingCase()
    {
        var store = CreateTestStore(false);
        Assert.Equal("initech", store.GetByIdentifierAsync("initech").Result.Identifier);
        Assert.Null(store.GetByIdentifierAsync("iNitEch").Result);
    }

    [Fact]
    public void FailIfAddingDuplicate()
    {
        var store = CreateTestStore();
        Assert.False(store.TryAddAsync(new TenantContext("initech", "initech", "Initech", null, null, null)).Result);
        Assert.False(store.TryAddAsync(new TenantContext("iNitEch", "iNitEch", "Initech", null, null, null)).Result);

        store = CreateTestStore(false);
        Assert.False(store.TryAddAsync(new TenantContext("initech", "initech", "Initech", null, null, null)).Result);
        Assert.True(store.TryAddAsync(new TenantContext("iNiTEch", "iNiTEch", "Initech", null, null, null)).Result);
    }

    [Fact]
    public void AddTenantToStore()
    {
        var store = CreateTestStore();

        Assert.Null(store.GetByIdentifierAsync("test").Result);
        Assert.True(store.TryAddAsync(new TenantContext("test", "test", "test", null, null, null)).Result);

        Assert.NotNull(store.GetByIdentifierAsync("test").Result);

        // test when already added
        Assert.False(store.TryAddAsync(new TenantContext("test", "test", "test", null, null, null)).Result);
    }

    [Fact]
    public void RemoveTenantFromStore()
    {
        var store = CreateTestStore();

        Assert.NotNull(store.GetByIdentifierAsync("initech").Result);
        Assert.True(store.TryRemoveAsync("initech").Result);

        Assert.Null(store.GetByIdentifierAsync("initech").Result);

        // test when already removed
        Assert.False(store.TryRemoveAsync("initech").Result);
    }

    [Fact]
    public void ThrowIfTenantIdentifierIsNullWhenGetting()
    {
        var store = CreateTestStore();

        var e = Assert.Throws<AggregateException>(() => store.GetByIdentifierAsync(null).Result);
        Assert.IsType<ArgumentNullException>(e.InnerException);
    }

    [Fact]
    public void ThrowIfTenantIdentifierIsNullWhenRemoving()
    {
        var store = new InMemoryMultiTenantStore();
        var e = Assert.Throws<ArgumentNullException>(() => store.TryRemoveAsync(null).Result);
    }

    [Fact]
    public void ThrowIfTenantContextIsNullWhenAdding()
    {
        var store = new InMemoryMultiTenantStore();
        var e = Assert.Throws<ArgumentNullException>(() => store.TryAddAsync(null).Result);
    }

    [Fact]
    public void ReturnNullIfTenantNotFound()
    {
        var store = CreateTestStore();

        Assert.Null(store.GetByIdentifierAsync("fake123").Result);
    }
}