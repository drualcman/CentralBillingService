global using Xunit;
global using NSubstitute;
global using NSubstitute.ExceptionExtensions;
global using Microsoft.Extensions.Options;

global using CentralBillingService.Domain.DTOs;
global using CentralBillingService.Domain.Entities;
global using CentralBillingService.Domain.Exceptions;
global using CentralBillingService.Domain.Interfaces;
global using CentralBillingService.Domain.Models;
global using CentralBillingService.Domain.Options;
global using CentralBillingService.Domain.Services;
global using CentralBillingService.Domain.ValueObjects;

global using CentralBillingService.Application.DTOs;
global using CentralBillingService.Application.Exceptions;
global using CentralBillingService.Application.Interfaces;
global using CentralBillingService.Application.UseCases;

global using CentralBillingService.Infrastructure.Entities;
global using CentralBillingService.Infrastructure.Hashing;
global using CentralBillingService.Infrastructure.Interfaces;
global using ISO9001.Core.Entities;

global using CentralBillingService.Persistence.SqlServer.Contetxs;
global using CentralBillingService.Persistence.SqlServer.Options;

global using CentralBillingService.Application.Models;

global using Microsoft.EntityFrameworkCore;

global using CentralBillingService.Tests.Helpers;
