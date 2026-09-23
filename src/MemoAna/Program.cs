using MemoAna.Components;
using MemoAna.Composition.Extensions;
using MudBlazor.Services;
await WebApplication.CreateBuilder()
    .RunMemoAnaAsync<Program, App>(
    builder => builder.Services.AddMudServices());