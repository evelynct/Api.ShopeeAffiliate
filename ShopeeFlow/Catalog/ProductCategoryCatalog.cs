namespace ShopeeFlow.Catalog;

public sealed record ProductCategory(int Id, string Name);

/// <summary>
/// Shopee productCatId catalog inferred from productOfferV2 samples.
/// Allowed = high-confidence leaf IDs that products in this flow should have.
/// Blocked = category IDs of prohibited verticals. Keyword-only hits in innocent categories are not listed.
/// Unconfirmed = IDs seen in samples whose meaning still needs a direct productCatId search.
/// </summary>
public static class ProductCategoryCatalog
{
    public static readonly IReadOnlyList<ProductCategory> Allowed =
    [
        new(101160, "Tapetes e passadeiras"),
        new(101153, "Flores e arranjos artificiais"),
        new(101161, "Vasos decorativos"),
        new(101164, "Toalhas de mesa"),
        new(101154, "Capas para móveis e eletrodomésticos"),
        new(101219, "Panelas"),
        new(101220, "Vidros de cozinha"),
        new(101237, "Utensílios de cozinha"),
        new(101173, "Armários de cozinha"),
        new(101239, "Xícaras e canecas"),
        new(101240, "Copos"),
        new(101243, "Pratos"),
        new(101242, "Tigelas e petisqueiras"),
        new(101247, "Acessórios de mesa"),
        new(101262, "Organizadores de parede"),
        new(101181, "Vasos para plantas"),
        new(100185, "Televisores"),
        new(100198, "Air fryer"),
        new(100193, "Liquidificador"),
        new(100191, "Chaleira elétrica"),
        new(100194, "Cafeteira elétrica"),
        new(100188, "Suportes e bases para TV")
    ];

    /// <summary>
    /// Product name fragments that indicate wedding/pix/signage junk outside the home niche.
    /// </summary>
    public static readonly IReadOnlyList<string> BlockedNameKeywords =
    [
        "pix",
        "casamento",
        "aliança",
        "alianças",
        "lua de mel",
        "operação lua",
        "operacao lua",
        "gravata",
        "noivos",
        "noiva",
        "placa aberto",
        "placa fechado",
        "qr code",
        "qrcode",
        "campainha",
        "porta aliança",
        "porta alianças",
        "urna pix",
        "cofre pix",
        "caixa pix"
    ];

    public static readonly IReadOnlyList<ProductCategory> Blocked =
    [
        new(100017, "Moda íntima e adulto"),
        new(100111, "Lingerie"),
        new(100382, "Roupa íntima"),
        new(100115, "Fantasias adultas"),
        new(100118, "Meia-calça e arrastão"),
        new(100388, "Produtos sexuais vestíveis"),
        new(100164, "Tatuagem íntima"),
        new(100019, "Saúde íntima"),
        new(100136, "Lubrificante e cuidados íntimos"),
        new(100660, "Cuidados íntimos masculinos"),
        new(100875, "Cremes íntimos masculinos"),
        new(100879, "Cremes de aumento / atraso sexual"),
        new(102002, "Óleos de massagem íntima"),
        new(102006, "Óleo kamasutra"),
        new(102009, "Óleo tântrico / erótico"),
        new(101245, "Canudos eróticos"),
        new(100002, "Suplementos (inclui vigor sexual e 'remédios')"),
        new(100003, "Suplementos de vitalidade"),
        new(100005, "Suplementos pré-treino / testosterona"),
        new(100006, "Suplementos (folha vigor / falso remédio)"),
        new(100007, "Suplementos em pó (vigor / libido)"),
        new(100629, "Alimentos e bebidas"),
        new(100651, "Bebidas alcoólicas"),
        new(100655, "Licores e destilados"),
        new(100826, "Bebida alcoólica"),
        new(100837, "Bebida mista alcoólica"),
        new(100862, "Licor"),
        new(100046, "Acessórios para fumar"),
        new(100631, "Pet"),
        new(100672, "Saúde pet"),
        new(100942, "Medicamentos veterinários"),
        new(101201, "Limpeza de armas / caça")
    ];

    public static readonly IReadOnlyList<ProductCategory> Unconfirmed =
    [
        new(100189, "Controle remoto como peça de TV"),
        new(100217, "Acessório eletrônico (ramo paralelo à TV)"),
        new(100291, "Controle Fire TV"),
        new(101376, "Blocos adesivos e marca-páginas"),
        new(100013, "Eletrônicos (árvore Fire TV / streaming)"),
        new(100042, "Acessórios eletrônicos (subcategoria isolada)"),
        new(100075, "Acessórios Fire TV"),
        new(100638, "Papelaria"),
        new(100734, "Papelaria — blocos e notas")
    ];

    public static readonly IReadOnlySet<int> AllowedIds = Allowed.Select(category => category.Id).ToHashSet();
    public static readonly IReadOnlySet<int> BlockedIds = Blocked.Select(category => category.Id).ToHashSet();
}
