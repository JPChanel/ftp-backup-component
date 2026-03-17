using app_ftp.Presentacion.Shared.Controls.DataTable;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Reflection;
using System.Text.Json;

namespace app_ftp.Presentacion.Shared
{
    public interface IDialogService
    {
        Task ShowError(string message, string title = "Error", string? primaryText = null, string? details = null);
    }

    public interface ILoadingService
    {
        bool IsLoading { get; }
        event Action<bool>? LoadingStateChanged;
        void StartLoading();
        void StopLoading();
    }

    public class WebApiException : Exception
    {
        public int StatusCode { get; init; }
        public string? ErrorDetails { get; init; }

        public WebApiException(string message, int statusCode = 0, string? errorDetails = null) : base(message)
        {
            StatusCode = statusCode;
            ErrorDetails = errorDetails;
        }
    }

    public abstract partial class ViewModelBase : ObservableObject
    {
        public readonly IDialogService DialogService;
        protected readonly ILoadingService LoadingService;

        public bool IsBusy => LoadingService?.IsLoading ?? false;
        public bool IsNotBusy => !IsBusy;

        /// <summary>
        /// Acción que se invoca cuando el ViewModel solicita cerrar la vista
        /// </summary>
        public Action? RequestClose { get; set; }

        protected ViewModelBase(IDialogService dialogService, ILoadingService loadingService)
        {
            DialogService = dialogService;
            LoadingService = loadingService;
            if (LoadingService != null)
            {
                LoadingService.LoadingStateChanged += OnLoadingStateChanged;
            }
        }

        protected ViewModelBase() : this(null, null)
        {
        }

        private void OnLoadingStateChanged(bool isLoading)
        {
            OnPropertyChanged(nameof(IsBusy));
            OnPropertyChanged(nameof(IsNotBusy));
        }

        protected async Task<bool> ExecuteSafeAsync(Func<Task> action, string defaultErrorMessage = "Ocurrió un error inesperado en el sistema.")
        {
            try
            {
                LoadingService?.StartLoading();
            }
            catch
            {
            }

            try
            {
                await action();

                try
                {
                    LoadingService?.StopLoading();
                }
                catch
                {
                }

                return true;
            }
            catch (WebApiException apiEx)
            {
                try
                {
                    LoadingService?.StopLoading();
                }
                catch
                {
                }

                if (DialogService != null)
                {
                    await DialogService.ShowError(
                        message: $" {apiEx.ErrorDetails ?? "Sin detalles."}",
                        title: $"Error de Servicio ({apiEx.StatusCode})",
                        primaryText: "Cerrar"
                    );
                }
                else
                {
                    System.Windows.MessageBox.Show(apiEx.ErrorDetails ?? "Sin detalles.", $"Error de Servicio ({apiEx.StatusCode})");
                }
                return false;
            }
            catch (Exception ex)
            {
                try
                {
                    LoadingService?.StopLoading();
                }
                catch
                {
                }

                if (DialogService != null)
                {
                    try
                    {
                        await DialogService.ShowError(message: ex.Message, title: "Error del Sistema", details: defaultErrorMessage);

                    }
                    catch
                    {
                        System.Windows.MessageBox.Show(ex.Message ?? "Sin detalles.", $"Error del Sistema ({defaultErrorMessage})");
                    }
                    ;

                }
                else
                {
                    System.Windows.MessageBox.Show(ex.Message, "Error del Sistema");
                }
                return false;
            }
        }

        protected async Task<bool> ExecuteDataLoadAsync<TEntity, TItemDto>(
            Func<Task<IEnumerable<TEntity>>> dataFetcher,
            Func<TEntity, TItemDto> dtoMapper,
            Func<TEntity, int> dataIdExtractor,
            Dictionary<int, TEntity> registrosCompletos,
            DataTableViewModel<TItemDto> tableViewModel,
            Action<List<TItemDto>>? statsUpdater = null,
            string loadingMessage = "Error al cargar registros")
            where TEntity : class
            where TItemDto : class
        {
            return await ExecuteSafeAsync(async () =>
            {
                var data = await dataFetcher();
                var dataList = data.ToList();

                var indexProperty = typeof(TItemDto).GetProperty("Index", BindingFlags.Public | BindingFlags.Instance);
                bool canSetIndex = indexProperty != null && indexProperty.CanWrite;
                registrosCompletos.Clear();

                var items = dataList.Select((reg, index) =>
                {
                    var id = dataIdExtractor(reg);
                    registrosCompletos[id] = reg;
                    var dto = dtoMapper(reg);

                    if (canSetIndex)
                    {
                        indexProperty!.SetValue(dto, index + 1);
                    }

                    return dto;
                }).ToList();

                tableViewModel.SetData(items);
                statsUpdater?.Invoke(items);

            }, loadingMessage);
        }

        protected T? GetValueFromObject<T>(object? extData, string key)
        {
            if (extData == null) return default;

            try
            {
                JsonElement json;

                if (extData is JsonElement je)
                    json = je;
                else if (extData is string str)
                    json = JsonDocument.Parse(str).RootElement;
                else
                    return default;

                if (json.TryGetProperty(key, out var element))
                    return element.Deserialize<T>();
            }
            catch { }

            return default;
        }

        /// <summary>
        /// Crea un AsyncRelayCommand que maneja automáticamente loading y errores
        /// </summary>
        protected CommunityToolkit.Mvvm.Input.IAsyncRelayCommand SafeCommand(Func<Task> execute)
        {
            return new CommunityToolkit.Mvvm.Input.AsyncRelayCommand(
                async () => await ExecuteSafeAsync(execute));
        }

        /// <summary>
        /// Crea un AsyncRelayCommand con parámetro que maneja automáticamente loading y errores
        /// </summary>
        protected CommunityToolkit.Mvvm.Input.IAsyncRelayCommand<T> SafeCommand<T>(Func<T?, Task> execute)
        {
            return new CommunityToolkit.Mvvm.Input.AsyncRelayCommand<T>(
                async (param) => await ExecuteSafeAsync(() => execute(param)));
        }
    }
}

