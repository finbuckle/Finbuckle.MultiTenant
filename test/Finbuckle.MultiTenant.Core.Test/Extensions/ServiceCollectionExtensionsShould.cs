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

using System.Linq;
using Finbuckle.MultiTenant;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ServiceCollectionExtensionsShould
{
    [Fact]
    public void RegisterTenantInfoInDI()
    {
        var services = new ServiceCollection();
        services.AddMultiTenantCore();

        var service = services.Where(s => s.Lifetime == ServiceLifetime.Scoped &&
                                            s.ServiceType == typeof(TenantInfo)).SingleOrDefault();

        Assert.NotNull(service);
    }

    [Fact]
    public void RegisterIMultiTenantContextInDI()
    {
        var services = new ServiceCollection();
        services.AddMultiTenantCore();

        var service = services.Where(s => s.Lifetime == ServiceLifetime.Scoped &&
                                            s.ServiceType == typeof(IMultiTenantContext)).SingleOrDefault();

        Assert.NotNull(service);
    }

    [Fact]
    public void RegisterIReadOnlyMultiTenantContextAccessorInDI()
    {
        var services = new ServiceCollection();
        services.AddMultiTenantCore();

        var service = services.Where(s => s.Lifetime == ServiceLifetime.Singleton &&
                                            s.ServiceType == typeof(IReadOnlyMultiTenantContextAccessor)).SingleOrDefault();

        Assert.NotNull(service);
    }

    [Fact]
    public void RegisterIReadOnlyMultiTenantContextInDI()
    {
        var services = new ServiceCollection();
        services.AddMultiTenantCore();

        var service = services.Where(s => s.Lifetime == ServiceLifetime.Transient &&
                                            s.ServiceType == typeof(IReadOnlyMultiTenantContext)).SingleOrDefault();

        Assert.NotNull(service);
    }
}