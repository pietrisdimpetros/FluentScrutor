# FluentScrutor

[![NuGet Version](https://img.shields.io/nuget/v/FluentScrutor.svg?style=flat-square)](https://www.nuget.org/packages/FluentScrutor/) [![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

A lightweight .NET library providing a convenient **fluent API** for registering decorator chains using the popular [Scrutor](https://github.com/khellang/Scrutor) library. It simplifies configuring the base service lifetime and helps prevent common configuration errors.

## Overview

While Scrutor provides powerful decoration capabilities, registering multiple decorators and configuring the base service can sometimes involve repetitive code. `FluentScrutor` wraps Scrutor's decoration features in an intuitive, chainable API, making your dependency injection setup cleaner and more readable.

## Features

* **Fluent Interface:** Register base services and multiple decorators using a clear, chainable syntax.
* **Lifetime Configuration:** Easily configure the `ServiceLifetime` (`Singleton`, `Scoped`, `Transient`) for the base service implementation within the fluent chain.
* **Duplicate Prevention:** Automatically prevents adding the *same* decorator type multiple times to a single decoration chain, throwing an exception to catch potential mistakes early.
* **Thread-Safe Configuration:** The builder used during configuration is thread-safe.
* **Built on Scrutor:** Leverages the robust and tested decoration mechanism provided by Scrutor.
* **Modern .NET:** Designed for use with `Microsoft.Extensions.DependencyInjection` in modern .NET applications.

## Requirements

* **.NET 8.0 SDK or later** (The library uses C# 12 features like primary constructors and collection expressions).
* [**Scrutor**](https://www.nuget.org/packages/Scrutor/) NuGet package (Tested with v6.x, ensure compatibility).
* `Microsoft.Extensions.DependencyInjection.Abstractions` (This is typically included when using ASP.NET Core or .NET Generic Host).
