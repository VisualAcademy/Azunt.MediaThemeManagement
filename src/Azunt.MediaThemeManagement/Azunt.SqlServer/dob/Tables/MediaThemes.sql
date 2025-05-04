-- [0][0] 테마: MediaThemes 
CREATE TABLE [dbo].[MediaThemes]
(
    [Id]           BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,               -- 고유 ID
    [Active]       BIT NOT NULL DEFAULT(1),                                 -- 활성 상태
    [Created]      DATETIMEOFFSET(7) NOT NULL DEFAULT SYSDATETIMEOFFSET(),  -- 생성 일시
    [CreatedBy]    NVARCHAR(255) NULL,                                      -- 생성자
    [Name]         NVARCHAR(255) NULL,                                      -- 테마 이름
    [IsDeleted]    BIT NOT NULL DEFAULT(0),                                 -- Soft Delete
    [DisplayOrder] INT NOT NULL DEFAULT(0)                                  -- 정렬 순서
);