namespace GerberParser.Commands.MacroPrimitives
{
    public class CommentMacroPrimitive : MacroPrimitive
    {
        public const string CODE = "0";
        public override string Id => CODE;

        public string Comment { get; private set; }

        protected override void Load(ref string str)
        {
            Comment = str;
            str = string.Empty;
        }

        public override void MultiplyBy(decimal mul)
        {

        }

        public override string GetStringInt()
        {
            return Comment;
        }
    }
}