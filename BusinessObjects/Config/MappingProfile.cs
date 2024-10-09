using AutoMapper;
using BusinessObjects.Entities;
using BusinessObjects.Models.Request;

namespace BusinessObjects.Config
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<Product, Product>();
		}
	}
}
