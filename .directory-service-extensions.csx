#r "nuget: System.DirectoryServices, 9.0.0"
#r "nuget: System.DirectoryServices.Protocols, 9.0.0"
#r "nuget: Lestaly, 0.69.0"
#nullable enable
using System.DirectoryServices.Protocols;
using Lestaly;

public static Task<DirectoryResponse> SendRequestAsync(this LdapConnection self, DirectoryRequest request, PartialResultProcessing partialMode = PartialResultProcessing.NoPartialResultSupport)
    => Task.Factory.FromAsync<DirectoryRequest, PartialResultProcessing, DirectoryResponse>(self.BeginSendRequest, self.EndSendRequest, request, partialMode, default(object));

public static async Task<SearchResultEntry> GetEntryAsync(this LdapConnection self, string dn)
{
    // Create a search request.
    var searchReq = new SearchRequest();
    searchReq.DistinguishedName = dn;
    searchReq.Scope = SearchScope.Base;

    // Request a search.
    var searchRsp = await self.SendRequestAsync(searchReq);
    if (searchRsp.ResultCode != 0) throw new PavedMessageException($"failed to search: {searchRsp.ErrorMessage}");
    var searchResult = searchRsp as SearchResponse ?? throw new PavedMessageException("unexpected result");

    return searchResult.Entries[0];
}

public static async Task<SearchResultEntry?> GetEntryOrDefaultAsync(this LdapConnection self, string dn)
{
    try
    {
        // Create a search request.
        var searchReq = new SearchRequest();
        searchReq.DistinguishedName = dn;
        searchReq.Scope = SearchScope.Base;

        // Request a search.
        var searchRsp = await self.SendRequestAsync(searchReq);
        if (searchRsp.ResultCode != 0) return default;
        if (searchRsp is not SearchResponse searchResult) return default;
        if (searchResult.Entries.Count <= 0) return default;
        return searchResult.Entries[0];
    }
    catch { return default; }
}

public static async Task<DirectoryResponse> CreateEntryAsync(this LdapConnection self, string dn, params DirectoryAttribute[] attributes)
{
    var createEntryReq = new AddRequest();
    createEntryReq.DistinguishedName = dn;
    foreach (var attr in attributes)
    {
        createEntryReq.Attributes.Add(attr);
    }

    var createEntryRsp = await self.SendRequestAsync(createEntryReq);
    if (createEntryRsp.ResultCode != 0) throw new PavedMessageException($"failed to search: {createEntryRsp.ErrorMessage}");

    return createEntryRsp;
}

public static async Task<DirectoryResponse> AddAttributeAsync(this LdapConnection self, string dn, string name, string value)
{
    var attrModify = new DirectoryAttributeModification();
    attrModify.Operation = DirectoryAttributeOperation.Add;
    attrModify.Name = name;
    attrModify.Add(value);

    var addAttrReq = new ModifyRequest();
    addAttrReq.DistinguishedName = dn;
    addAttrReq.Modifications.Add(attrModify);

    var addAttrRsp = await self.SendRequestAsync(addAttrReq);
    if (addAttrRsp.ResultCode != 0) throw new PavedMessageException($"failed to search: {addAttrRsp.ErrorMessage}");

    return addAttrRsp;
}

public static string? GetAttributeSingleValue(this SearchResultEntry self, string name)
{
    var attr = self.Attributes[name];
    if (attr == null) return null;
    if (attr.Count <= 0) return null;
    if (1 < attr.Count) throw new PavedMessageException("multiple attribute value");

    return attr[0] as string;
}

public static IEnumerable<string> EnumerateAttributeValues(this SearchResultEntry self, string name)
{
    var attr = self.Attributes[name];
    if (attr == null) yield break;

    for (var i = 0; i < attr.Count; i++)
    {
        if (attr[i] is string value)
        {
            yield return value;
        }
    }
}

public static DirectoryAttributeModification AddAttributeAdd(this ModifyRequest self, string name, string value)
{
    var attr = new DirectoryAttributeModification();
    attr.Operation = DirectoryAttributeOperation.Add;
    attr.Name = name;
    attr.Add(value);

    self.Modifications.Add(attr);

    return attr;
}

public static DirectoryAttributeModification AddAttributeReplace(this ModifyRequest self, string name, string value)
{
    var attr = new DirectoryAttributeModification();
    attr.Operation = DirectoryAttributeOperation.Replace;
    attr.Name = name;
    attr.Add(value);

    self.Modifications.Add(attr);

    return attr;
}
