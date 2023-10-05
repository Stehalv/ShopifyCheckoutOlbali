using Common;
using HtmlAgilityPack;
using System;
using System.IO;
using System.Web.Mvc;
using System.Web;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;


public static class ControllerExtensions
{
    /// <summary>
    /// Method Used to Render a Partial View with Model intact as a string for use in a JSON request
    /// </summary>
    /// <param name="viewName">Path of view that you are attempting to call.</param>
    /// <param name="model">The model data that the view should expect to receive.</param>
    /// <returns>Html string of partial view.</returns>
    public static string RenderPartialViewToString(this Controller controller, string viewName, object model)
    {
        if (string.IsNullOrEmpty(viewName))
            viewName = controller.ControllerContext.RouteData.GetRequiredString("action");

        controller.ViewData.Model = model;


        using (var writer = new StringWriter())
        {
            ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
            ViewContext viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, writer);
            viewResult.View.Render(viewContext, writer);

            return writer.GetStringBuilder().ToString();
        }
    }

    public static string RenderPartialViewToString(this Controller controller, string viewName, object model, ViewDataDictionary viewData)
    {
        if (string.IsNullOrEmpty(viewName))
            viewName = controller.ControllerContext.RouteData.GetRequiredString("action");

        controller.ViewData.Model = model;
        foreach (var item in viewData)
        {
            controller.ViewData.Add(item);
        }

        using (var writer = new StringWriter())
        {
            ViewEngineResult viewResult = ViewEngines.Engines.FindPartialView(controller.ControllerContext, viewName);
            ViewContext viewContext = new ViewContext(controller.ControllerContext, viewResult.View, controller.ViewData, controller.TempData, writer);
            viewResult.View.Render(viewContext, writer);

            return writer.GetStringBuilder().ToString();
        }
    }

    /// <summary>
    /// Determines if the provided view exists for the this controller
    /// </summary>
    /// <param name="view">The name of the view</param>
    /// <returns></returns>
    public static bool ViewExists(this Controller controller, string view)
    {
        ViewEngineResult result = ViewEngines.Engines.FindView(controller.ControllerContext, view, null);
        return (result.View != null);
    }


    /// <summary>
    /// Renders content from the marketingsite, based on the link clicked in the UI which is not found in this project
    /// </summary>
    /// <param name="controller"></param>
    /// <returns></returns>
    public static string RenderContent(this Controller controller, bool IsContent, string id)
    {
        var Request = controller.Request;
        //get the page
        var Url = GetRemoteUrl(Request);
        var document = GetDocument(Request, Url);
        ReplaceLinks(Request, document);
        //document = CleanupInHeader(Request, document);
        return document.DocumentNode.OuterHtml;
    }
    /// <summary>
    /// Puts content generated from this project inside the frame from Marketingsite
    /// </summary>
    /// <param name="controller"></param>
    /// <param name="content"The content to inject></param>
    /// <returns></returns>
    public static string RenderContent(this Controller controller, string content, string Masterpage = "Masterpage")
    {
        //Get Documents
        var url = GlobalSettings.ReplicatedSites.ReplicatedHomePage;
        var Template = GetDocument(controller.Request, url);
        var thisUrl = controller.Request.Url;
        //Pull in elements for masterpage for this solution
        var MasterUrl = thisUrl.Scheme + Uri.SchemeDelimiter + thisUrl.Host + ":" + thisUrl.Port + "/home/" + Masterpage; ;
        var Master = GetDocument(controller.Request, MasterUrl);
        ReplaceLinks(controller.Request, Template);
        //Insert the content
        var node = Template.DocumentNode.SelectSingleNode("//section");
        node.InnerHtml = content;
        RemoveNodes(Template, "//script[@src]");
        Template = CleanupInHeader(controller.Request, Template);
        Template = InjectCodeBefore(Template, Master, "head");
        Template = InjectCodeAfter(Template, Master, "body");
        return Template.DocumentNode.OuterHtml;
    }
    private static string GetRemoteUrl(HttpRequestBase Request)
    {
        var path = (Request.RequestContext.RouteData.Values["id"] != null) ? Request.RequestContext.RouteData.Values["id"].ToString() : "";
        return new UriBuilder()
        {
            Scheme = "https",
            Host = GlobalSettings.ReplicatedSites.RepHost,
            Path = "/" + path
        }.Uri.ToString();
    }
    private static HtmlDocument GetDocument(HttpRequestBase Request, string Url)
    {
        var doc = new HtmlDocument();
        using (WebClient webClient = new WebClient())
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var stream = webClient.OpenRead(Url);
            using (StreamReader sr = new StreamReader(stream))
            {
                var page = sr.ReadToEnd();
                doc.LoadHtml(page);
            }
            stream.Close();
        }
        return doc;
    }
    private static HtmlDocument InjectCodeBefore(HtmlDocument Target, HtmlDocument Source, string node)
    {
        var TargetPage = new HtmlDocument();
        TargetPage.LoadHtml(Target.DocumentNode.OuterHtml);
        var SourcePage = new HtmlDocument();
        SourcePage.LoadHtml(Source.DocumentNode.OuterHtml);
        var SourceChildNodes = SourcePage.DocumentNode.SelectSingleNode("//" + node).ChildNodes;
        var firstNode = TargetPage.DocumentNode.SelectSingleNode("//" + node).FirstChild;
        foreach (var _node in SourceChildNodes)
        {
            TargetPage.DocumentNode.SelectSingleNode("//" + node).InsertBefore(_node, firstNode);
        }
        return TargetPage;
    }
    private static HtmlDocument InjectCodeAfter(HtmlDocument Target, HtmlDocument Source, string node)
    {
        var TargetPage = new HtmlDocument();
        TargetPage.LoadHtml(Target.DocumentNode.OuterHtml);
        var SourcePage = new HtmlDocument();
        SourcePage.LoadHtml(Source.DocumentNode.OuterHtml);
        var SourceChildNodes = SourcePage.DocumentNode.SelectSingleNode("//" + node).ChildNodes;
        TargetPage.DocumentNode.SelectSingleNode("//" + node).AppendChildren(SourceChildNodes);
        return TargetPage;
    }
    private static HtmlDocument ReplaceLinks(HttpRequestBase Request, HtmlDocument document)
    {
        var thisHost = Request.Url.Host;
        var repHost = GlobalSettings.ReplicatedSites.RepHost;
        var webAlias = Request.RequestContext.RouteData.Values["webalias"].ToString();
        foreach (HtmlNode link in document.DocumentNode.SelectNodes("//a[@href]"))
        {
            try
            {
                HtmlAttribute att = link.Attributes["href"];
                if (att.Value.Contains(repHost))
                {
                    var newUrl = ReplaceHost(Request, att.Value, thisHost, webAlias);
                    att.Value = newUrl;
                }
                if (att.Value.ToLower() == "/enrollment")
                {
                    if (thisHost == "localhost")
                    {
                        var newUrl = Request.Url.Scheme + "://" + thisHost + ":" + Request.Url.Port + "/" + webAlias + att.Value;
                        att.Value = newUrl;
                    }
                    else
                    {
                        var newUrl = Request.Url.Scheme + "://" + thisHost + "/" + webAlias + att.Value;
                        att.Value = newUrl;
                    }
                }
                if (att.Value[0] == '/')
                {
                    if (thisHost == "localhost")
                    {
                        var newUrl = Request.Url.Scheme + "://" + thisHost + ":" + Request.Url.Port + "/" + webAlias + "/content" + att.Value;
                        att.Value = newUrl;
                    }
                    else
                    {
                        var newUrl = Request.Url.Scheme + "://" + thisHost + "/" + webAlias + "/content" + att.Value;
                        att.Value = newUrl;
                    }
                }
                if (att.Value.Contains("make-appointment"))
                {
                    if (thisHost == "localhost")
                    {
                        var newUrl = Request.Url.Scheme + "://" + thisHost + ":" + Request.Url.Port + "/" + webAlias + "/account/login";
                        att.Value = newUrl;
                    }
                    else
                    {
                        var newUrl = Request.Url.Scheme + "://" + thisHost + "/" + webAlias + "/account/login";
                        att.Value = newUrl;
                    }
                }
            }
            catch
            {

            }
        }
        return document;
    }
    private static HtmlDocument RemoveNodes(HtmlDocument document, string removeTag)
    {
        foreach (HtmlNode node in document.DocumentNode.SelectNodes(removeTag))
        {
            node.Remove();
        }
        return document;
    }
    /// <summary>
    /// Replaces the host in the ireaction needed
    /// </summary>
    /// <param name="Request"></param>
    /// <param name="original"></param>
    /// <param name="newHostName"></param>
    /// <param name="webAlias"></param>
    /// <returns></returns>
    private static string ReplaceHost(this HttpRequestBase Request, string original, string newHostName, string webAlias = "")
    {
        try
        {
            var builder = new UriBuilder(original);
            builder.Host = newHostName;
            builder.Scheme = (newHostName == "localhost") ? "http" : builder.Scheme;
            builder.Port = (newHostName != "localhost") ? 443 : Request.Url.Port;
            if (Request.Url.Host == newHostName)
            {
                builder.Path = webAlias + "/content" + builder.Path;
            }
            return builder.Uri.ToString();
        }
        catch
        {
            return original;
        }
    }
    /// <summary>
    /// Switches out the remote Fusion css with a local copy.
    /// Also injects the Head from the MasterpageContent (_MasterpageContentLayut) into the end of the header tag of the remote masterpage template
    /// </summary>
    /// <param name="Request"></param>
    /// <param name="doc"></param>
    /// <returns></returns>
    private static HtmlDocument CleanupInHeader(HttpRequestBase Request, HtmlDocument doc)
    {
        try
        {
            var thisUrl = Request.Url;
            //Pull in elements for masterpage for this solution
            var SourceUrl = thisUrl.Scheme + Uri.SchemeDelimiter + thisUrl.Host + ":" + thisUrl.Port + "/home/masterpagecontent";
            var Source = GetDocument(Request, SourceUrl);
            var removeCSS = doc.GetElementbyId("fusion-dynamic-css-css");
            removeCSS.Remove();
            return InjectCodeAfter(doc, Source, "head");
        }
        catch
        {
            return doc;
        }

    }
}