using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace CWFlgma.Infrastructure.MongoDB.Documents;

public class DesignDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = null!;

    [BsonElement("documentId")]
    public string DocumentId { get; set; } = null!;

    [BsonElement("version")]
    public int Version { get; set; }

    [BsonElement("pages")]
    public List<Page> Pages { get; set; } = new();

    [BsonElement("components")]
    public List<Component> Components { get; set; } = new();

    [BsonElement("styles")]
    public List<Style> Styles { get; set; } = new();

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; }
}

public class Page
{
    [BsonElement("id")]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("backgroundColor")]
    public string BackgroundColor { get; set; } = "#FFFFFF";

    [BsonElement("layers")]
    public List<Layer> Layers { get; set; } = new();
}

public class Layer
{
    [BsonElement("id")]
    public string Id { get; set; } = null!;

    [BsonElement("type")]
    public string Type { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("transform")]
    public Transform Transform { get; set; } = new();

    [BsonElement("style")]
    public LayerStyle Style { get; set; } = new();

    [BsonElement("text")]
    public TextProperties? Text { get; set; }

    [BsonElement("image")]
    public ImageProperties? Image { get; set; }

    [BsonElement("layout")]
    public LayoutProperties? Layout { get; set; }

    [BsonElement("constraints")]
    public Constraints? Constraints { get; set; }

    [BsonElement("visible")]
    public bool Visible { get; set; } = true;

    [BsonElement("locked")]
    public bool Locked { get; set; }

    [BsonElement("opacity")]
    public double Opacity { get; set; } = 1;

    [BsonElement("children")]
    public List<Layer>? Children { get; set; }
}

public class Transform
{
    [BsonElement("x")]
    public double X { get; set; }

    [BsonElement("y")]
    public double Y { get; set; }

    [BsonElement("width")]
    public double Width { get; set; }

    [BsonElement("height")]
    public double Height { get; set; }

    [BsonElement("rotation")]
    public double Rotation { get; set; }

    [BsonElement("scaleX")]
    public double ScaleX { get; set; } = 1;

    [BsonElement("scaleY")]
    public double ScaleY { get; set; } = 1;
}

public class LayerStyle
{
    [BsonElement("fill")]
    public Fill? Fill { get; set; }

    [BsonElement("stroke")]
    public Stroke? Stroke { get; set; }

    [BsonElement("borderRadius")]
    public BorderRadius? BorderRadius { get; set; }

    [BsonElement("shadow")]
    public List<Shadow>? Shadow { get; set; }

    [BsonElement("blur")]
    public Blur? Blur { get; set; }
}

public class Fill
{
    [BsonElement("type")]
    public string Type { get; set; } = "solid";

    [BsonElement("color")]
    public string? Color { get; set; }

    [BsonElement("opacity")]
    public double Opacity { get; set; } = 1;

    [BsonElement("gradient")]
    public Gradient? Gradient { get; set; }
}

public class Gradient
{
    [BsonElement("type")]
    public string Type { get; set; } = "linear";

    [BsonElement("stops")]
    public List<GradientStop> Stops { get; set; } = new();

    [BsonElement("angle")]
    public double Angle { get; set; }
}

public class GradientStop
{
    [BsonElement("color")]
    public string Color { get; set; } = null!;

    [BsonElement("position")]
    public double Position { get; set; }
}

public class Stroke
{
    [BsonElement("color")]
    public string Color { get; set; } = "#000000";

    [BsonElement("width")]
    public double Width { get; set; }

    [BsonElement("align")]
    public string Align { get; set; } = "center";
}

public class BorderRadius
{
    [BsonElement("topLeft")]
    public double TopLeft { get; set; }

    [BsonElement("topRight")]
    public double TopRight { get; set; }

    [BsonElement("bottomLeft")]
    public double BottomLeft { get; set; }

    [BsonElement("bottomRight")]
    public double BottomRight { get; set; }
}

public class Shadow
{
    [BsonElement("x")]
    public double X { get; set; }

    [BsonElement("y")]
    public double Y { get; set; }

    [BsonElement("blur")]
    public double Blur { get; set; }

    [BsonElement("spread")]
    public double Spread { get; set; }

    [BsonElement("color")]
    public string Color { get; set; } = "rgba(0,0,0,0.25)";
}

public class Blur
{
    [BsonElement("type")]
    public string Type { get; set; } = "gaussian";

    [BsonElement("radius")]
    public double Radius { get; set; }
}

public class TextProperties
{
    [BsonElement("content")]
    public string Content { get; set; } = null!;

    [BsonElement("fontFamily")]
    public string FontFamily { get; set; } = "Roboto";

    [BsonElement("fontSize")]
    public double FontSize { get; set; } = 16;

    [BsonElement("fontWeight")]
    public int FontWeight { get; set; } = 400;

    [BsonElement("fontStyle")]
    public string FontStyle { get; set; } = "normal";

    [BsonElement("textAlign")]
    public string TextAlign { get; set; } = "left";

    [BsonElement("lineHeight")]
    public double LineHeight { get; set; } = 1.5;

    [BsonElement("letterSpacing")]
    public double LetterSpacing { get; set; }

    [BsonElement("textDecoration")]
    public string TextDecoration { get; set; } = "none";
}

public class ImageProperties
{
    [BsonElement("resourceId")]
    public string ResourceId { get; set; } = null!;

    [BsonElement("fit")]
    public string Fit { get; set; } = "cover";

    [BsonElement("position")]
    public string Position { get; set; } = "center";
}

public class LayoutProperties
{
    [BsonElement("mode")]
    public string Mode { get; set; } = "none";

    [BsonElement("gap")]
    public double Gap { get; set; }

    [BsonElement("padding")]
    public Padding? Padding { get; set; }

    [BsonElement("alignment")]
    public string Alignment { get; set; } = "start";
}

public class Padding
{
    [BsonElement("top")]
    public double Top { get; set; }

    [BsonElement("right")]
    public double Right { get; set; }

    [BsonElement("bottom")]
    public double Bottom { get; set; }

    [BsonElement("left")]
    public double Left { get; set; }
}

public class Constraints
{
    [BsonElement("horizontal")]
    public string Horizontal { get; set; } = "left";

    [BsonElement("vertical")]
    public string Vertical { get; set; } = "top";
}

public class Component
{
    [BsonElement("id")]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("mainComponentId")]
    public string? MainComponentId { get; set; }

    [BsonElement("variants")]
    public List<ComponentVariant>? Variants { get; set; }
}

public class ComponentVariant
{
    [BsonElement("id")]
    public string Id { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("properties")]
    public Dictionary<string, object>? Properties { get; set; }
}

public class Style
{
    [BsonElement("id")]
    public string Id { get; set; } = null!;

    [BsonElement("type")]
    public string Type { get; set; } = null!;

    [BsonElement("name")]
    public string Name { get; set; } = null!;

    [BsonElement("value")]
    public BsonDocument? Value { get; set; }
}
