namespace app_ftp.Presentacion.Shared.Utility;

public static class ObjectMapper
{
    public static void CopyProperties<TSource, TTarget>(TSource source, TTarget target)
    {
        if (source == null || target == null) return;

        var propsSource = typeof(TSource).GetProperties();
        var propsTarget = typeof(TTarget).GetProperties();

        foreach (var prop in propsSource)
        {
            var targetProp = propsTarget.FirstOrDefault(p =>
                p.Name == prop.Name &&
                p.PropertyType == prop.PropertyType &&
                p.CanWrite);

            if (targetProp != null)
                targetProp.SetValue(target, prop.GetValue(source));
        }
    }
}

