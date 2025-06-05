using AngleSharp.Dom;
using HtmlAgilityPack;

public static class HtmlNodeExtensions
{
    public static string? GetImageUrl(this HtmlNode node, string basePath, string invalidNameImg = "data:image/")
    {
        if (node.IsValidUrl("data-src", invalidNameImg))
            return node.GetAbsoluteUrl("data-src", basePath);

        if (node.IsValidUrl("data-lazy-src", invalidNameImg))
            return node.GetAbsoluteUrl("data-lazy-src", basePath);

        if (node.IsValidUrl("srcset", invalidNameImg))
        {
            var srcset = node.GetAbsoluteUrl("srcset", basePath);
            return srcset.Split(' ')[0]; // Toma la primera URL del srcset
        }

        if (node.IsValidUrl("src", invalidNameImg))
            return node.GetAbsoluteUrl("src", basePath);

        return "";
    }

    public static bool IsValidUrl(this HtmlNode node, string attrName, string invalidNameImg = "data:image/")
    {
        if (!node.Attributes.Contains(attrName))
            return false;

        var attrValue = node.GetAttributeValue(attrName, "");
        return !attrValue.StartsWith(invalidNameImg, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetAbsoluteUrl(this HtmlNode node, string attrName, string basePath)
    {
        var value = node.GetAttributeValue(attrName, "");
        try
        {
            var uri = new Uri(value, UriKind.RelativeOrAbsolute);
            if (!uri.IsAbsoluteUri && Uri.TryCreate(new Uri(basePath), uri, out Uri? resolved))
                return resolved.ToString();

            return uri.ToString();
        }
        catch
        {
            return value;
        }
    }

    /********************AngleSharp*****************/
    public static string? GetImageUrl(this IElement element, string invalidNameImg = "data:image/")
    {
        if (element.IsValidUrl("data-src", invalidNameImg))
            return element.GetAbsoluteUrl("data-src");

        if (element.IsValidUrl("data-lazy-src", invalidNameImg))
            return element.GetAbsoluteUrl("data-lazy-src");

        if (element.IsValidUrl("srcset", invalidNameImg))
        {
            var srcset = element.GetAbsoluteUrl("srcset");
            return srcset.Split(' ')[0];
        }

        if (element.IsValidUrl("src", invalidNameImg))
            return element.GetAbsoluteUrl("src");
        return "";
    }

    public static bool IsValidUrl(this IElement element, string attrName, string invalidNameImg = "data:image/")
    {
        if (!element.HasAttribute(attrName))
            return false;

        var attrValue = element.GetAttribute(attrName) ?? "";
        return !attrValue.StartsWith(invalidNameImg ?? "", StringComparison.OrdinalIgnoreCase);
    }

    public static string GetAbsoluteUrl(this IElement element, string attrName)
    {
        var value = element.GetAttribute(attrName) ?? "";

        try
        {
            var uri = new Uri(value, UriKind.RelativeOrAbsolute);

            if (!uri.IsAbsoluteUri && element.Owner?.Url is string baseUrl &&
                Uri.TryCreate(new Uri(baseUrl), uri, out var resolved))
            {
                return resolved.ToString();
            }

            return uri.ToString();
        }
        catch
        {
            return value;
        }
    }
}