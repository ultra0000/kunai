namespace Kunai.Generic
{
    internal interface IImportExport<T> where T : class
    {
        public KunaiProjectFile Import(string in_Path);
        T Export(KunaiProjectFile in_File);
    }
}