namespace FishShop.API.Shared;

public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public int StatusCode { get; }
    
    protected internal Result(bool isSuccess, string? error, int statusCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        StatusCode = statusCode;
    }
    
    
    public static Result Success() => new(true, null, StatusCodes.Status200OK);
    public static Result Created() => new(true, null, StatusCodes.Status201Created);
    public static Result NotFound(string error) => new(false, error, StatusCodes.Status404NotFound);
    public static Result BadRequest(string error) => new(false, error, StatusCodes.Status400BadRequest);

    public static Result<T> Success<T>(T value) => new(value, true, null, StatusCodes.Status200OK);
    public static Result<T> Created<T>(T value) => new(value, true, null, StatusCodes.Status201Created);
    public static Result<T> NotFound<T>(string error) => new(default, false, error, StatusCodes.Status404NotFound);
    public static Result<T> BadRequest<T>(string error) => new(default, false, error, StatusCodes.Status400BadRequest);

    public virtual IResult Resolve()
    {
        return Results.StatusCode(StatusCode);
    }
}

public class Result<T>:Result
{
    public T Value { get; }

    protected internal Result(T value, bool isSuccess, string? error, int statusCode)
    : base(isSuccess, error, statusCode)
    {
        Value = value;
    }
    
    public override IResult Resolve()
    {
        return StatusCode switch
        {
            StatusCodes.Status200OK => Results.Ok(Value),
            StatusCodes.Status201Created => Results.Created(),
            StatusCodes.Status404NotFound => Results.NotFound(Error),
            StatusCodes.Status400BadRequest => Results.BadRequest(Error)
        };
    }
}