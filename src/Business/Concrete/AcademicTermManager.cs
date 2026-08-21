using Business.Abstract;
using Business.Constants;
using Business.DTOs.Reference;
using Core.DataAccess;
using Core.Utilities.Results;
using Entities;

namespace Business.Concrete;

public sealed class AcademicTermManager(IEntityRepository<AcademicTerm> academicTermRepository, IUnitOfWork unitOfWork) : IAcademicTermService
{
    private const int DefaultPageSize = 20;
    private const int MaxPageSize = 100;

    public async Task<IDataResult<PagedResult<AcademicTermListItemDto>>> GetTermsPagedAsync(
        int pageIndex, int pageSize, CancellationToken cancellationToken = default)
    {
        var paged = await academicTermRepository
            .GetListPagedAsync(pageIndex, ClampPageSize(pageSize), cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var items = paged.Items
            .OrderByDescending(t => t.StartDateUtc)
            .Select(t => new AcademicTermListItemDto
            {
                Id = t.Id, Name = t.Name, StartDateUtc = t.StartDateUtc, EndDateUtc = t.EndDateUtc, IsCurrent = t.IsCurrent,
            })
            .ToList();

        return DataResult<PagedResult<AcademicTermListItemDto>>.Success(
            new PagedResult<AcademicTermListItemDto>(items, paged.TotalCount, paged.PageIndex, paged.PageSize));
    }

    public async Task<IDataResult<int>> CreateTermAsync(CreateAcademicTermRequestDto request, CancellationToken cancellationToken = default)
    {
        var name = request.Name.Trim();

        var existing = await academicTermRepository.GetAsync(t => t.Name == name, cancellationToken).ConfigureAwait(false);
        if (existing is not null)
        {
            return DataResult<int>.Conflict(Messages.AcademicTermNameTaken);
        }

        var term = new AcademicTerm { Name = name, StartDateUtc = request.StartDateUtc, EndDateUtc = request.EndDateUtc, IsCurrent = false };
        await academicTermRepository.AddAsync(term, cancellationToken).ConfigureAwait(false);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return DataResult<int>.Success(term.Id);
    }

    public async Task<IResult> SetCurrentAsync(int termId, CancellationToken cancellationToken = default)
    {
        var target = await academicTermRepository.GetAsync(t => t.Id == termId, cancellationToken).ConfigureAwait(false);
        if (target is null)
        {
            return Result.NotFound(Messages.AcademicTermNotFound);
        }

        if (target.IsCurrent)
        {
            return Result.Success(Messages.AcademicTermAlreadyCurrent);
        }

        // Sıra kasıtlı: önce eski güncel dönem false yapılıp kaydedilir, ancak ondan sonra hedef
        // true yapılıp kaydedilir — aksi hâlde tek SaveChanges'in ürettiği iki UPDATE'in sırası
        // garanti edilmez ve IX_AcademicTerms_IsCurrent (filtreli unique index) ihlal edilebilir.
        var currentTerm = await academicTermRepository.GetAsync(t => t.IsCurrent, cancellationToken).ConfigureAwait(false);
        if (currentTerm is not null)
        {
            currentTerm.IsCurrent = false;
            academicTermRepository.Update(currentTerm);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        target.IsCurrent = true;
        academicTermRepository.Update(target);
        await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result.Success(Messages.AcademicTermSetCurrent);
    }

    private static int ClampPageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(pageSize, MaxPageSize);
}
