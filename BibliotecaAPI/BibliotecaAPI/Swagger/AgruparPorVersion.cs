using Microsoft.AspNetCore.Mvc.ApplicationModels;

namespace BibliotecaAPI.Swagger
{
    public class AgruparPorVersion : IControllerModelConvention
    {
        public void Apply(ControllerModel controller)
        {
            var namespaceDelControlador = controller.ControllerType.Namespace;
            var version = namespaceDelControlador!.Split('.').Last().ToLower();
            controller.ApiExplorer.GroupName = version;
        }
    }
}
