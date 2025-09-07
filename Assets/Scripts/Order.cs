[System.Serializable]
public class Order
{
    public string genre;     // e.g. "Romance", "Fantasy"
    public string cover;     // e.g. "Flowers", "Dragon"

    public Order(string genre, string cover)
    {
        this.genre = genre;
        this.cover = cover;
    }

    public override string ToString()
    {
        return $"{genre} book with {cover} cover";
    }
}
