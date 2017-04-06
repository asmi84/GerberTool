namespace GerberParser.Commands
{
    public interface IContainsUnits
    {
        void MultiplyBy(decimal mul);

        void MoveBy(decimal offsetX, decimal offsetY);
    }
}