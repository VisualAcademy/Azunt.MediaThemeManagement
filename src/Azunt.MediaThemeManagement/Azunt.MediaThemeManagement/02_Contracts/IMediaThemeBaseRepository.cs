using Azunt.Repositories;

namespace Azunt.MediaThemeManagement;

/// <summary>
/// 기본 CRUD 작업을 위한 MediaTheme 전용 저장소 인터페이스
/// </summary>
public interface IMediaThemeBaseRepository : IRepositoryBase<MediaTheme, long>
{
}