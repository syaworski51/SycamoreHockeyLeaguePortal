namespace SycamoreHockeyLeaguePortal.Interfaces
{
    public interface IDTOConverter<TObject, TDTO>
    {
        TDTO ConvertObjectToDTO(TObject @object);
        IEnumerable<TDTO> ConvertObjectsToDTOs(IEnumerable<TObject> objects);
        TObject ConvertDTOToObject(TDTO dto);
        IEnumerable<TObject> ConvertDTOsToObjects(IEnumerable<TDTO> DTOs);
    }
}
